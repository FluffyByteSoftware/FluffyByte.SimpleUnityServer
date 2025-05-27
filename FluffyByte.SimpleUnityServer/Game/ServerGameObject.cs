namespace FluffyByte.SimpleUnityServer.Game
{
    using System;
    using System.Numerics;

    internal class ServerGameObject
    {
        public Guid Guid { get; private set; }
        public int Id { get; private set; } = _id++;

        private static int _id = 0;

        public string Name { get; set; } = "ServerGameObject";
        public float CurrentMovementSpeed { get; set; }
        public float CurrentRotationSpeed { get; set; }
        public Quaternion CurrentRotation { get; set; }
        public Vector3 CurrentPosition { get; set; }

        public ServerGameObject()
        {
            Guid = Guid.NewGuid();

            CurrentMovementSpeed = 0f;
            CurrentRotationSpeed = 0f;
            CurrentRotation = Quaternion.Identity;
            CurrentPosition = Vector3.Zero;
        }

        public ServerGameObject(int id, string name, float movementSpeed, float rotationSpeed, Quaternion rotation, Vector3 position)
        {
            Guid = Guid.NewGuid();
            Id = id;
            Name = name;
            CurrentMovementSpeed = movementSpeed;
            CurrentRotationSpeed = rotationSpeed;
            CurrentRotation = rotation;
            CurrentPosition = position;
        }

        public string SerializationString()
        {
            return $"{Guid} {Id} {CurrentPosition.X} {CurrentPosition.Y} {CurrentPosition.Z} " +
              $"{CurrentRotation.X} {CurrentRotation.Y} {CurrentRotation.Z} {CurrentRotation.W}";
        }

        public static ServerGameObject Parse(string line)
        {
            string[] tokens = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            if(tokens.Length < 9) 
                throw new FormatException("Invalid serialization format for ServerGameObject.");

            return new ServerGameObject
            {
                Guid = Guid.Parse(tokens[0]),
                Id = int.Parse(tokens[1]),
                CurrentPosition = new Vector3(
                    float.Parse(tokens[2]), 
                    float.Parse(tokens[3]), 
                    float.Parse(tokens[4])
                ),
                CurrentRotation = new Quaternion(
                    float.Parse(tokens[5]), 
                    float.Parse(tokens[6]), 
                    float.Parse(tokens[7]), 
                    float.Parse(tokens[8])
                )
            };
        }

        public override string ToString()
        {
            return $"ServerGameObject: {Name} (ID: {Id}, Guid: {Guid}) at Position: {CurrentPosition}, " +
                "Rotation: {CurrentRotation}, Movement Speed: {CurrentMovementSpeed}, Rotation Speed: {CurrentRotationSpeed}";
        }

        
    }
}
