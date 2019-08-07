namespace MrTe.Threading.Tasks
{
	public class Task{

        public static void Delay(int timeout, int token)
        {
            int i = 0;
            while (timeout > i) {

                System.Threading.Thread.Sleep(1);
                i++;
            }
		
	   }	
	}
}
