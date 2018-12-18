using FluentScheduler;
using LiteDB;
using log4net;
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
        private readonly ILog _logger;
        #endregion

        #region Constructor
        public LifeTimeStorage(ILog logger, string name, int expiration, int ttlCheck)
        {
            // check whenever some of these values are wrong
            // and store information in our log.
            if (string.IsNullOrEmpty(name) || expiration <= 0 || ttlCheck < 0)
            {
                _logger.Error("LifeTimeStorage.Ctor() - some of the arguments weren't initialized as expected.");
                return;
            }

            this._logger = logger;
            this._expirationTime = expiration;
            this._databaseName = name;
            this._dataBaseFile = AppDomain.CurrentDomain.BaseDirectory + $"{_databaseName}.db";

            _logger.Info("LifeTimeStorage Created");

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
            try
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
            catch (Exception ex)
            {
                _logger.Error("LifeTimeStorage.DeleteExpiredEntries() - " + ex.Message);
            }
        }
        #endregion

        #region Public Methods
        public void Put(string key, T tag)
        {
            try
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
            catch (Exception ex)
            {
                _logger.Error("LifeTimeStorage.Put() - " + ex.Message);
            }
        }

        public bool Remove(string key)
        {
            try
            {
                lock (_lock)
                {
                    using (var db = new LiteDatabase(_dataBaseFile))
                    {
                        _fileHashCollection = db.GetCollection<FileHashTableEntry<T>>(_databaseName);
                        var results = _fileHashCollection.FindOne(x => x.Key.Equals(key));
                        return results != null && _fileHashCollection.Delete(results.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error("LifeTimeStorage.Remove() - " + ex.Message);
                return false;
            }
        }

        public bool Exists(string key)
        {
            try
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
            catch (Exception ex)
            {
                _logger.Error("LifeTimeStorage.Exists() - " + ex.Message);
                return false;
            }
        }

        public T Get(string key)
        {
            try
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
            catch (Exception ex)
            {
                _logger.Error("LifeTimeStorage.Get() - " + ex.Message);
                return default(T);
            }
        }

        public List<T> GetValueList()
        {
            try
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
            catch (Exception ex)
            {
                _logger.Error("LifeTimeStorage.GetValueList() - " + ex.Message);
                return null;
            }
        }

        public Dictionary<string, T> GetDictionary()
        {
            try
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
            catch (Exception ex)
            {
                _logger.Error("LifeTimeStorage.GetDictionary() - " + ex.Message);
                return null;
            }
        }

        public void Clear()
        {
            try
            {
                lock (_lock)
                {
                    using (var db = new LiteDatabase(_dataBaseFile))
                    {
                        db.DropCollection(_databaseName);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error("LifeTimeStorage.Clear() - " + ex.Message);
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
