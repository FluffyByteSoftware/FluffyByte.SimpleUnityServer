using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using FluffyByte.SimpleUnityServer.Core;
using FluffyByte.SimpleUnityServer.Utilities;

namespace FluffyByte.SimpleUnityServer.Game.Managers
{
    internal class GameObjectManager : CoreServiceTemplate
    {
        public override string Name => "GameObjectManager";

        private readonly ThreadSafeList<ServerGameObject> _objects = [];

        public void RegisterObject(ServerGameObject obj) => _objects.Add(obj);
        public void UnregisterObject(ServerGameObject obj) => _objects.Remove(obj);

        public IEnumerable<ServerGameObject> AllObjects => _objects;

        public string SerializeAllObjects()
        {
            StringBuilder sb = new();

            foreach(ServerGameObject obj in _objects)
            {
                sb.AppendLine(obj.SerializationString());
            }

            return sb.ToString();
        }

        public void DeSerializeAllObjects(string data)
        {
            _objects.Clear();

            string[] lines = data.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries);
            
            foreach (string line in lines)
            {
                try
                {
                    ServerGameObject obj = ServerGameObject.Parse(line);
                    
                    _objects.Add(obj);
                }
                catch (Exception ex)
                {
                    Scribe.Error(ex);
                }
            }
        }

        public async static Task<List<ServerGameObject>> ParseObjectList(string payload)
        {
            List<ServerGameObject> result = [];
            
            string[] lines = payload.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                try
                {
                    var obj = ServerGameObject.Parse(line); // You must implement Parse as shown before
                    result.Add(obj);
                }
                catch (Exception ex)
                {
                    await Scribe.ErrorAsync(ex);

                    // Optionally log or handle bad lines
                    // e.g., Console.WriteLine($"Failed to parse line: {line} -- {ex.Message}");
                }
            }

            return result;
        }
    }
}
