using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace DiscordHackWeek2019.Data
{
    public interface IDataProvider
    {
        IEnumerable<T> GetAll<T>(string collection);
        T GetFirstOrDefault<T>(string collection, Expression<Func<T, bool>> predicate);
        T GetById<T>(ulong id);
        IEnumerable<T> GetWhere<T>(string collection, Expression<Func<T, bool>> predicate);
        ulong Insert<T>(string collection, T item);
        int Insert<T>(string collection, IEnumerable<T> items);
        bool Update<T>(string collection, T item);
        bool Update<T>(string collection, ulong id, T item);
        int Update<T>(string collection, IEnumerable<T> items);
        bool Rename(string collection, string newName);
        bool Delete(string collection);
        bool Delete(string collection, ulong id);
        bool Exists(string collection);
    }
}
