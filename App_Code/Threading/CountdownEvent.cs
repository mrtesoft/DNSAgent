namespace MrTe.Threading.Tasks
{
	public class CountdownEvent{
        static int Count = 0;
        static int Index = 0;
        public CountdownEvent() { 
        }

        public CountdownEvent(int count)
        {
            Count = count;
        }
        public void Signal() {
            Index++;
        }
        public void Wait() {

            while (Count > Index) {
                System.Threading.Thread.Sleep(10);
            }
        
        }
	}
}