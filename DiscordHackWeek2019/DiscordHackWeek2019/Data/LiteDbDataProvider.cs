using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace DiscordHackWeek2019.Data
{
    public class LiteDbDataProvider : IDataProvider
    {
        private readonly string path;

        public LiteDbDataProvider(string path)
        {
            this.path = path;
        }

        public bool Delete(string collection)
        {
            using (var db = new LiteDatabase(path))
            {
                return db.DropCollection(collection);
            }
        }

        public bool Delete(string collection, ulong id)
        {
            using (var db = new LiteDatabase(path))
            {
                return db.GetCollection(collection).Delete(id);
            }
        }

        public bool Exists(string collection)
        {
            using (var db = new LiteDatabase(path))
            {
                return db.CollectionExists(collection);
            }
        }

        public IEnumerable<T> GetAll<T>(string collection)
        {
            using (var db = new LiteDatabase(path))
            {
                return db.GetCollection<T>().FindAll();
            }
        }

        public T GetById<T>(ulong id)
        {
            using (var db = new LiteDatabase(path))
            {
                return db.GetCollection<T>().FindById(id);
            }
        }

        public T GetFirstOrDefault<T>(string collection, Expression<Func<T, bool>> predicate)
        {
            using (var db = new LiteDatabase(path))
            {
                return db.GetCollection<T>(collection).FindOne(predicate);
            }
        }

        public IEnumerable<T> GetWhere<T>(string collection, Expression<Func<T, bool>> predicate)
        {
            using (var db = new LiteDatabase(path))
            {
                return db.GetCollection<T>(collection).Find(predicate);
            }
        }

        public ulong Insert<T>(string collection, T item)
        {
            using (var db = new LiteDatabase(path))
            {
                return db.GetCollection<T>(collection).Insert(item);
            }
        }

        public int Insert<T>(string collection, IEnumerable<T> items)
        {
            using (var db = new LiteDatabase(path))
            {
                return db.GetCollection<T>(collection).Insert(items);
            }
        }

        public bool Rename(string collection, string newName)
        {
            using (var db = new LiteDatabase(path))
            {
                return db.RenameCollection(collection, newName);
            }
        }

        public bool Update<T>(string collection, T item)
        {
            using (var db = new LiteDatabase(path))
            {
                return db.GetCollection<T>(collection).Update(item);
            }
        }

        public bool Update<T>(string collection, ulong id, T item)
        {
            using (var db = new LiteDatabase(path))
            {
                return db.GetCollection<T>(collection).Update(id, item);
            }
        }

        public int Update<T>(string collection, IEnumerable<T> items)
        {
            using (var db = new LiteDatabase(path))
            {
                return db.GetCollection<T>(collection).Update(items);
            }
        }
    }
}
