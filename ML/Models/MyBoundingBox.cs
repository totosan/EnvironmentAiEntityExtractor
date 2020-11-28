namespace EntityExtractor.ML.Model
{
    public class MyBoundingBox{
        public MyBoundingBox(float top, float left, float height, float width)
        {
            this.top = top;
            this.left = left;
            this.width = width;
            this.height = height;
        }

        public float top { get; }
        public float left { get; }
        public float width { get; }
        public float height { get; }

        public int[] ConvertToIntArray(){
            return new int[]{(int)top,(int)left,(int)height,(int)width};
        }
        public override string ToString()
        {
            return $"top: {top} left: {left} height: {height} width: {width}";
        }
    }
}