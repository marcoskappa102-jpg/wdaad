namespace MMOServer.Models
{
    public class Position
    {
        public float x { get; set; }
        public float y { get; set; }
        public float z { get; set; }

        public Position() { }

        public Position(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
    }
}