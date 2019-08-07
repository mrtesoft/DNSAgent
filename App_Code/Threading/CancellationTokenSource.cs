namespace MrTe.Threading.Tasks
{
    public class CancellationTokenSource {
        public static int Index=0;
        public CancellationTokenSource() {
            Index++;
            _Token = Index;
        }
        bool _IsCancellationRequested=false;
        public bool IsCancellationRequested {
            get { return _IsCancellationRequested; }
            set { _IsCancellationRequested = value; }
        }
         int _Token;
        public int Token {
            get { return _Token; }
            set { _Token = value; }
        }
        public void Cancel() {
            IsCancellationRequested = true;
        }
    
    
    }


}