﻿using SonicLairCli;

using SonicLair.Lib.Infrastructure;


using System.Diagnostics;

using Terminal.Gui;

namespace SonicLair.Cli
{
    public class SonicLairListView<T> : ListView
    {
        public SonicLairListView()
        {
            ColorScheme = SonicLairControls.ListViewColorScheme;
        }

        private Action<SonicLairListView<T>>? _onLeave = null;

        public void SetOnLeave(Action<SonicLairListView<T>> action)
        {
            _onLeave = action;
        }

        public override bool OnLeave(View view)
        {
            if (_onLeave != null)
            {
                _onLeave(this);
            }
            return base.OnLeave(view);
        }

        public void ScrollTo(int index)
        {
            GetCurrentHeight(out int height);
            if (Source.Count <= height)
            {
                return;
            }
            ScrollUp(Source.Count);
            int slack = height / 2;
            if(index < slack)
            {
                return;
            }
            ScrollDown((index - height + slack).Clamp(0, Source.Count - height));
        }

        private readonly Dictionary<Key, Action> _hotkeys = new Dictionary<Key, Action>();

        public void RegisterHotKey(Key key, Action action)
        {
            _hotkeys.Add(key, action);
        }

        private DateTime lastPressed = DateTime.Now;
        private string searchTerm = "";

        private readonly List<Key> movementKeys = new List<Key>()
        {
            Key.CursorDown,
            Key.CursorUp,
            Key.CursorLeft,
            Key.CursorRight,
            Key.Home,
            Key.End,
            Key.PageUp,
            Key.PageDown,
        };

        private bool Process(KeyEvent e, Func<KeyEvent, bool> def)
        {
            Debug.WriteLine(e.IsCtrl);
            if (e.IsCtrl && _hotkeys.ContainsKey(e.Key))
            {
                _hotkeys[e.Key]();
                return true;
            }
            if (e.IsCtrl)
            {
                // I don't wanna process Ctrl hotkeys here
                return false;
            }
            if (e.Key == Key.Tab)
            {
                // reserved for tabbing
                return false;
            }
            if (e.Key == Key.Backspace)
            {
                // reserved for back
                return false;
            }
            if (e.Key == Key.Space)
            {
                // reserved for pause & play
                return false;
            }
            if (e.Key == Key.Backspace)
            {
                // reserved for back
                return false;
            }
            if (!e.IsCtrl && !e.IsAlt && !e.IsShift && e.Key != Key.Enter && !movementKeys.Contains(e.Key))
            {
                var diff = (DateTime.Now - lastPressed).Milliseconds;
                Debug.WriteLine(diff);
                if (diff > 500)
                {
                    Debug.WriteLine("Clearing searchterm");
                    searchTerm = "";
                }
                lastPressed = DateTime.Now;
                searchTerm += e.Key.ToString();
                Debug.WriteLine($"Searching for {searchTerm}");
                var list = (List<T>?)Source?.ToList();
                if (list == null)
                {
                    return true;
                }
                var item = list.FirstOrDefault(s => s?.ToString()?.ToLowerInvariant().StartsWith(searchTerm.ToLowerInvariant()) ?? false);
                if (item != null)
                {
                    SelectedItem = list.IndexOf(item);
                }
                ScrollUp(list.Count);
                ScrollDown(SelectedItem);
                Application.Refresh();
                return true;
            }
            return def(e);
        }

        public override bool ProcessKey(KeyEvent kb)
        {
            return Process(kb, base.ProcessKey);
        }

        public override bool ProcessHotKey(KeyEvent keyEvent)
        {
            return Process(keyEvent, base.ProcessHotKey);
        }
    }
}