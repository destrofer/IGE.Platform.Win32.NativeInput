/*
 * Author: Viacheslav Soroka
 * 
 * This file is part of IGE <https://github.com/destrofer/IGE>.
 * 
 * IGE is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * IGE is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 * 
 * You should have received a copy of the GNU Lesser General Public License
 * along with IGE.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;

using IGE.Input;

namespace IGE.Platform.Win32 {
	/// <summary>
	/// </summary>
	public class KeyboardDevice : IKeyboardDevice {
		public event KeyEventHandler KeyDownEvent;
		public event KeyEventHandler KeyUpEvent;
		public event KeyEventHandler KeyPressEvent;
		
		protected bool[] m_Keys;
		protected bool[] m_PrevKeys;
		protected Queue<KeyAndChar> m_KeyQueue;
		
		private static KeyboardDevice Instance;
		
		static KeyboardDevice() {
			Instance = new KeyboardDevice();
		}
		
		public static KeyboardDevice GetInstance() {
			return Instance;
		}

		protected KeyboardDevice() {
			m_Keys = new bool[256];
			m_PrevKeys = new bool[256];
			m_KeyQueue = new Queue<KeyAndChar>();
			
			Win32Application.ProcessWindowMessageEvent += OnWndProc;
			Application.PostIdleEvent += SaveState; 
		}
		
		public string DeviceName { get { return "Keyboard"; } }
		public string DeviceId { get { return "native-keyboard-combined"; } }
		
		private void OnWndProc(WindowMessageEventArgs args) {
			if( args.PreventsDefault )
				return;
			switch( args.uMsg ) {
				case WindowMessageEnum.KEYDOWN: {
					OnKeyDown((Key)args.wParam);
					args.ReturnValue = (IntPtr)1;
					args.PreventDefault();
					break;
				}
				case WindowMessageEnum.KEYUP: {
					OnKeyUp((Key)args.wParam);
					args.ReturnValue = (IntPtr)1;
					args.PreventDefault();
					break;
				}
				case WindowMessageEnum.CHAR: {
					OnKeyPress((Key)args.lParam, (char)args.wParam);
					args.ReturnValue = (IntPtr)1;
					args.PreventDefault();
					break;
				}
			}
		}
		
		protected virtual void OnKeyDown(Key key) {
			if( KeyDownEvent != null )
				KeyDownEvent(new KeyEventArgs(this, key));
			m_Keys[(int)key] = true;
		}
		
		protected virtual void OnKeyUp(Key key) {
			if( KeyUpEvent != null )
				KeyUpEvent(new KeyEventArgs(this, key));
			m_Keys[(int)key] = false;
		}
		
		protected virtual void OnKeyPress(Key key, char keyChar) {
			if( KeyPressEvent != null )
				KeyPressEvent(new KeyEventArgs(this, key, keyChar));
			if( m_KeyQueue.Count < 100 )
				m_KeyQueue.Enqueue( new KeyAndChar { Key = key, Char = keyChar } );
		}
		
		public virtual bool IsDown(Key key) { return m_Keys[(int)key]; }
		public virtual bool IsUp(Key key) { return !m_Keys[(int)key]; }
		public virtual bool WasDown(Key key) { return m_PrevKeys[(int)key]; }
		public virtual bool WasUp(Key key) { return !m_PrevKeys[(int)key]; }
		public virtual bool Pressed(Key key) { return m_Keys[(int)key] && !m_PrevKeys[(int)key]; }
		public virtual bool Released(Key key) { return !m_Keys[(int)key] && m_PrevKeys[(int)key]; }
		
		public virtual KeyAndChar ReadKey() { return (m_KeyQueue.Count == 0) ? KeyAndChar.Zero : m_KeyQueue.Dequeue(); }
		
		public virtual void SaveState() { m_Keys.CopyTo(m_PrevKeys, 0); }
		
		public virtual void Dispose() {
			Application.PostIdleEvent -= SaveState; 
			Win32Application.ProcessWindowMessageEvent -= OnWndProc;
		}
	}
}
