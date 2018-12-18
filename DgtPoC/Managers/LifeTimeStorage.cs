using FluentScheduler;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DgtPoC
{
    public class LifeTimeStorage<T> : ILifeTimeList<T>
    {
        #region Events & Delegates
        public event CustomEventHandler<T>.FileHashTableRemoveEntryDel FileHashTableRemoveEntry;
        #endregion

        #region Private Fields
        private LiteCollection<FileHashTableEntry<T>> _fileHashCollection;
        private readonly object _lock = new object();
        private readonly string _dataBaseFile;
        private readonly string _databaseName;
        private readonly int _expirationTime;
        #endregion

        #region Constructor
        public LifeTimeStorage(string name, int expiration, int ttlCheck)
        {
            // check whenever some of these values are wrong
            // TODO: throw exception and block other operations.
            if (string.IsNullOrEmpty(name) || expiration <= 0 || ttlCheck < 0)
                return;

            this._expirationTime = expiration;
            this._databaseName = name;
            this._dataBaseFile = AppDomain.CurrentDomain.BaseDirectory + $"{_databaseName}.db";

            // open database (or create if doesn't exist)
            // using will take care of disposing objects
            using (var db = new LiteDatabase(_dataBaseFile))
            {
                // get a collection (or create, if doesn't exist)
                // creates a new permanent index in all documents inside 
                _fileHashCollection = db.GetCollection<FileHashTableEntry<T>>(_databaseName);
                _fileHashCollection.EnsureIndex(x => x.Key);
            }

            // ttlCheck determines how often given function will invoke
            if (ttlCheck > 0)
            {
                JobManager.AddJob(DeleteExpiredEntries, (s) => s.ToRunEvery(ttlCheck).Seconds());
            }
        }
        #endregion

        #region Private Methods
        private void DeleteExpiredEntries()
        {
            lock (_lock)
            {
                using (var db = new LiteDatabase(_dataBaseFile))
                {
                    _fileHashCollection = db.GetCollection<FileHashTableEntry<T>>(_databaseName);

                    var results = _fileHashCollection.Find(x => x.DeleteDate >= DateTime.UtcNow);

                    if (results == null) return;

                    foreach (FileHashTableEntry<T> fileHashTableContent in results)
                    {
                        FileHashTableRemoveEntry?.Invoke(fileHashTableContent.Key, fileHashTableContent);
                        _fileHashCollection.Delete(fileHashTableContent.Id);
                    }
                }
            }
        }
        #endregion

        #region Public Methods
        public void Put(string key, T tag)
        {
            lock (_lock)
            {
                if (key == null) return;

                using (var db = new LiteDatabase(_dataBaseFile))
                {
                    _fileHashCollection = db.GetCollection<FileHashTableEntry<T>>(_databaseName);
                    var results = _fileHashCollection.FindOne(x => x.Key.Equals(key));
                    if (results != null)
                    {
                        results.Tag = tag;
                        results.DeleteDate = DateTime.UtcNow + TimeSpan.FromSeconds(_expirationTime);
                        _fileHashCollection.Update(results);
                    }
                    else
                    {
                        var ob = new FileHashTableEntry<T>
                        {
                            Key = key,
                            Tag = tag,
                            DeleteDate = DateTime.UtcNow + TimeSpan.FromSeconds(_expirationTime)
                        };

                        // binary-encoded format. Extends the JSON model to provide
                        // additional data types, ordered fields and it's quite
                        // efficient for encoding and decoding within different languages
                        var mapper = new BsonMapper();
                        var doc = mapper.ToDocument(typeof(FileHashTableEntry<T>), ob);

                        _fileHashCollection.Insert(ob);
                    }
                }
            }
        }

        public bool Remove(string key)
        {
            lock(_lock)
            {
                using (var db = new LiteDatabase(_dataBaseFile))
                {
                    _fileHashCollection = db.GetCollection<FileHashTableEntry<T>>(_databaseName);
                    var results = _fileHashCollection.FindOne(x => x.Key.Equals(key));
                    return results != null && _fileHashCollection.Delete(results.Id);
                }
            }
        }

        public bool Exists(string key)
        {
            lock (_lock)
            {
                using (var db = new LiteDatabase(_dataBaseFile))
                {
                    _fileHashCollection = db.GetCollection<FileHashTableEntry<T>>(_databaseName);
                    var results = _fileHashCollection.FindOne(x => x.Key.Equals(key));

                    if (results != null)
                        return true;
                }
                return false;
            }
        }

        public T Get(string key)
        {
            lock (_lock)
            {
                using (var db = new LiteDatabase(_dataBaseFile))
                {
                    _fileHashCollection = db.GetCollection<FileHashTableEntry<T>>(_databaseName);
                    return _fileHashCollection.FindOne(x => x.Key.Equals(key)).Tag;
                }
            }
        }

        public List<T> GetValueList()
        {
            lock (_lock)
            {
                using (var db = new LiteDatabase(_dataBaseFile))
                {
                    _fileHashCollection = db.GetCollection<FileHashTableEntry<T>>(_databaseName);

                    FileHashTableEntry<T>[] all = _fileHashCollection.FindAll().ToArray();

                    return new List<T>(all.Select(x => x.Tag));
                }
            }
        }

        public Dictionary<string, T> GetDictionary()
        {
            lock (_lock)
            {
                using (var db = new LiteDatabase(_dataBaseFile))
                {
                    _fileHashCollection = db.GetCollection<FileHashTableEntry<T>>(_databaseName);

                    FileHashTableEntry<T>[] all = _fileHashCollection.FindAll().ToArray();

                    return all.ToDictionary(fileHashTableContent => fileHashTableContent.Key,
                        fileHashTableContent => fileHashTableContent.Tag);
                }
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                using (var db = new LiteDatabase(_dataBaseFile))
                {
                    db.DropCollection(_databaseName);
                }
            }
        }

        public void RemoveJobs()
        {
            lock (_lock)
            {
                JobManager.RemoveAllJobs();
            }
        }
        #endregion
    }
}
