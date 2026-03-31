using MongoDB.Driver;
using MoneyManager.Domain.Exceptions;
using MoneyManager.Domain.Interfaces;
using MoneyManager.Infrastructure.Data;

namespace MoneyManager.Infrastructure.Repositories;

public class Repository<T> : IRepository<T> where T : class
{
    protected readonly IMongoCollection<T> Collection;

    public Repository(MongoContext context, string collectionName)
    {
        Collection = context.GetCollection<T>(collectionName);
    }

    public virtual async Task<T?> GetByIdAsync(string id)
    {
        var filter = Builders<T>.Filter.Eq("_id", MongoDB.Bson.ObjectId.Parse(id));
        return await Collection.Find(filter).FirstOrDefaultAsync();
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        return await Collection.Find(Builders<T>.Filter.Empty).ToListAsync();
    }

    public virtual async Task<T> AddAsync(T entity)
    {
        await Collection.InsertOneAsync(entity);
        return entity;
    }

    public virtual async Task<T> UpdateAsync(T entity)
    {
        var entityType = entity.GetType();
        var id = entityType.GetProperty("Id")?.GetValue(entity);
        var filter = Builders<T>.Filter.Eq("_id", MongoDB.Bson.ObjectId.Parse(id?.ToString() ?? ""));

        var versionProp = entityType.GetProperty("Version");
        if (versionProp != null)
        {
            var currentVersion = (int)(versionProp.GetValue(entity) ?? 1);
            filter = filter & (Builders<T>.Filter.Eq("version", currentVersion) | Builders<T>.Filter.Not(Builders<T>.Filter.Exists("version")));
            versionProp.SetValue(entity, currentVersion + 1);

            var result = await Collection.ReplaceOneAsync(filter, entity);
            if (result.ModifiedCount == 0)
            {
                versionProp.SetValue(entity, currentVersion);
                throw new ConcurrencyException();
            }
        }
        else
        {
            await Collection.ReplaceOneAsync(filter, entity);
        }

        return entity;
    }

    public virtual async Task DeleteAsync(string id)
    {
        var filter = Builders<T>.Filter.Eq("_id", MongoDB.Bson.ObjectId.Parse(id));
        await Collection.DeleteOneAsync(filter);
    }
}
