using System;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

namespace PooketSlug
{
    /// <summary>
    /// Reuses short-lived entities (bullets, explosions, debris) instead of creating and destroying a GameObject
    /// for each one, which churns the garbage collector on WebGL. Each <see cref="World"/> has its own pools, and
    /// they go when it does: disposing the world destroys everything under its root, pooled objects included.
    /// </summary>
    public sealed class EntityPool<T> where T : Entity
    {
        readonly ObjectPool<T> m_Pool;
        int m_Made, m_Used;

        /// <param name="create">Makes a new, blank entity; the caller sets it up after <see cref="Get"/>.</param>
        /// <param name="maxIdle">How many released entities to keep for reuse; the rest are destroyed.</param>
        public EntityPool(Func<T> create, int maxIdle)
        {
            m_Pool = new ObjectPool<T>(() =>
                {
                    m_Made++;
                    return create();
                },
                actionOnGet: e =>
                {
                    e.Removed = false;
                    e.Go.SetActive(true);
                },
                actionOnRelease: e => e.Go.SetActive(false),
                actionOnDestroy: e => Object.Destroy(e.Go),
                collectionCheck: true, // throws if an entity is released twice
                defaultCapacity: 32,
                maxSize: maxIdle);
        }

        public T Get()
        {
            T entity = m_Pool.Get();
            // With ?log=game:debug, each doubling reports how many were used and how few had to be made.
            if (++m_Used >= 64 && (m_Used & (m_Used - 1)) == 0) Log.Game.Debug($"{typeof(T).Name} pool: {m_Used} used, {m_Made} made");
            return entity;
        }

        public void Release(T entity) => m_Pool.Release(entity);
    }
}
