using PoeHUD.Controllers;
using PoeHUD.Poe;
using PoeHUD.Poe.Elements;
using System;
using System.Collections.Generic;
using PoeHUD.Hud;

namespace PoeHUD.Models
{
    using Poe.RemoteMemoryObjects;

    public sealed class EntityListWrapper
    {
        private readonly GameController gameController;
        private readonly HashSet<string> ignoredEntities;
        private Dictionary<uint, EntityWrapper> entityCache;

        public EntityListWrapper(GameController gameController)
        {
            this.gameController = gameController;
            entityCache = new Dictionary<uint, EntityWrapper>();
            ignoredEntities = new HashSet<string>();
            gameController.Area.AreaChange += OnAreaChanged;
        }

        public ICollection<EntityWrapper> Entities => entityCache.Values;

        private EntityWrapper player;
        public Dictionary<int, int> PlayerStats { get; private set; } = new Dictionary<int, int>();
        public EntityWrapper Player
        {
            get
            {
                if (player == null)
                    UpdatePlayer();
                return player;
            }
        }

        public event Action<EntityWrapper> EntityAdded;
        public event Action<EntityWrapper> EntityAddedAny = delegate { };

        public event Action<EntityWrapper> EntityRemoved;

        private void OnAreaChanged(AreaController area)
        {
            ignoredEntities.Clear();
            PlayerStats.Clear();
            RemoveOldEntitiesFromCache();
        }

        private void RemoveOldEntitiesFromCache()
        {
            foreach (var current in Entities)
            {
                EntityRemoved?.Invoke(current);
                current.IsInList = false;
            }
            entityCache.Clear();
        }

        public void RefreshState()
        {
            UpdatePlayer();
            if(player.IsAlive && player.IsValid && player.HasComponent<Poe.Components.Stats>())
                UpdatePlayerStats();

            if (gameController.Area.CurrentArea == null)
                return;

            Dictionary<uint, Entity> newEntities = gameController.Game.IngameState.Data.EntityList.EntitiesAsDictionary;
            PluginLogger.LogError($"gameController.Game.IngameState: {gameController.Game.IngameState.Address:X} IngameData: {gameController.Game.IngameState.Data.Address:X} EntityList: {gameController.Game.IngameState.Data.EntityList.Address:X}", 5);
            PluginLogger.LogError($"Returned entities as dictionary: {newEntities.Count}", 5);
            var newCache = new Dictionary<uint, EntityWrapper>();
            foreach (var keyEntity in newEntities)
            {
                if (!keyEntity.Value.IsValid)
                {
                    PluginLogger.LogError($"Entity Address: {keyEntity.Value.Address:X} Id: {keyEntity.Key}  Path: {keyEntity.Value.Path} ... is invalid", 5);
                    continue;
                }

                var entityID = keyEntity.Key;
                string uniqueEntityName = keyEntity.Value.Path + entityID;

                if (ignoredEntities.Contains(uniqueEntityName))
                {
                    PluginLogger.LogError($"Entity {keyEntity.Key} Path {keyEntity.Value.Path} is ignored", 5);
                    continue;
                }

                if (entityCache.ContainsKey(entityID) && entityCache[entityID].IsValid)
                {
                    newCache.Add(entityID, entityCache[entityID]);
                    entityCache[entityID].IsInList = true;
                    entityCache.Remove(entityID);

                    PluginLogger.LogError($"Entity {keyEntity.Key} Path {keyEntity.Value.Path} already in cache", 5);
                    continue;
                }

                var entity = new EntityWrapper(gameController, keyEntity.Value);

                EntityAddedAny(entity);
                if (entity.Path.StartsWith("Metadata/Effects") || ((entityID & 0x80000000L) != 0L) ||
                    entity.Path.StartsWith("Metadata/Monsters/Daemon"))
                {
                    ignoredEntities.Add(uniqueEntityName);
                    PluginLogger.LogError($"Entity {keyEntity.Key} Path {keyEntity.Value.Path} unique ignored", 5);
                    continue;
                }

                PluginLogger.LogError($"Entity {keyEntity.Key} Path {keyEntity.Value.Path} unique ignored", 5);
                EntityAdded?.Invoke(entity);
                newCache.Add(entityID, entity);
            }
            RemoveOldEntitiesFromCache();
            entityCache = newCache;
        }

        private void UpdatePlayer()
        {
            long address = gameController.Game.IngameState.Data.LocalPlayer.Address;
            if ((player == null) || (player.Address != address))
            {
                player = new EntityWrapper(gameController, address);
            }
        }
        private void UpdatePlayerStats()
        {
            var stats = player.GetComponent<Poe.Components.Stats>();
            PlayerStats = stats.StatDictionary;
        }
        public EntityWrapper GetEntityById(uint id)
        {
            EntityWrapper result;
            return entityCache.TryGetValue(id, out result) ? result : null;
        }
    }
}