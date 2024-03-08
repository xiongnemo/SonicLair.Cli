using NStack;

using Terminal.Gui;

namespace SonicLair.Cli
{
    public class SonicLairWindow : Window
    {

        public SonicLairWindow(ustring title = null) : base(title)
        {
        }

        public SonicLairWindow(Rect frame, ustring title = null) : base(frame, title)
        {
        }

        public SonicLairWindow(ustring title = null, int padding = 0) : base(title, padding)
        {
        }

        public SonicLairWindow(Rect frame, ustring title = null, int padding = 0) : base(frame, title, padding)
        {
        }

        private readonly Dictionary<Key, Action> _hotkeys = new();
        private readonly Dictionary<Key, Action> _keys = new();
        public void RegisterHotKey(Key key, Action action)
        {
            _hotkeys.Add(key, action);
        }

        public void RegisterKey(Key key, Action action)
        {
            _keys.Add(key, action);
        }

        public override bool ProcessHotKey(KeyEvent e)
        {
            if (e.IsCtrl && _hotkeys.ContainsKey(e.Key))
            {
                _hotkeys[e.Key]();
                return true;
            }
            return base.ProcessHotKey(e);
        }

        public override bool ProcessKey(KeyEvent e)
        {
            if (_keys.ContainsKey(e.Key))
            {
                _keys[e.Key]();
                return true;
            }
            return base.ProcessKey(e);
        }
    }
}