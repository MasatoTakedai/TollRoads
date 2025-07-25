using Colossal.Serialization.Entities;
using Unity.Collections;
using Unity.Entities;

namespace TollRoads
{
    public struct TollLane : IComponentData, IQueryTypeParameter, ISerializable
    {
        public int toll;
        public int nightToll;
        public float truckMultiplier;
        public int revenue; // only to be used for UI
        public int nextRevenue; // only to be used for UI
        public int volume; // only to be used for UI
        public int nextVolume; // only to be used for UI
        public NativeHashSet<Entity> vehicles;

        public TollLane(int toll)
        {
            this.toll = toll;
            nightToll = (int)(toll * 0.5f);
            truckMultiplier = 1.5f;
            revenue = 0;
            nextRevenue = 0;
            volume = 0;
            nextVolume = 0;
            vehicles = new NativeHashSet<Entity>(0, Allocator.Persistent);
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(toll);
            writer.Write(nightToll);
            writer.Write(truckMultiplier);
            writer.Write(revenue);
            writer.Write(nextRevenue);
            writer.Write(volume);
            writer.Write(nextVolume);
            writer.Write(vehicles.Count);
            writer.Write(vehicles.ToNativeArray(Allocator.Persistent));
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out toll);
            reader.Read(out nightToll);
            reader.Read(out truckMultiplier);
            reader.Read(out revenue);
            reader.Read(out nextRevenue);
            reader.Read(out volume);
            reader.Read(out nextVolume); 

            reader.Read(out int count);
            NativeArray<Entity> array = new NativeArray<Entity>(count, Allocator.Temp);
            reader.Read(array);
            vehicles = new NativeHashSet<Entity>(count, Allocator.Persistent);
            for (int i = 0; i < count; i++)
                vehicles.Add(array[i]);
        }
    }
}
