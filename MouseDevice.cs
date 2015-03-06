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

using IGE.Input;

namespace IGE.Platform.Win32 {
	/// <summary>
	/// </summary>
	public class MouseDevice : IMouseDevice {
		private static MouseDevice Instance;
		
		private Win32NativeWindow m_Window;
		public INativeWindow Window {
			get { return m_Window; }
			set { SetWindow((Win32NativeWindow)value); }
		}
		
		static MouseDevice() {
			Instance = new MouseDevice();
		}
		
		public static MouseDevice GetInstance() {
			return Instance;
		}
		
		protected MouseDevice() {
			Reset();
			Application.PreIdleEvent += OnBeforeFrame;
			Application.ActivateAppEvent += OnAppActivate;
			Application.DeactivateAppEvent += OnAppDeactivate;
			Win32Application.ProcessWindowMessageEvent += OnWndProc;
		}
		
		public string DeviceName { get { return "Mouse"; } }
		public string DeviceId { get { return "native-mouse-combined"; } }
		
		protected bool m_Visible = true;
		public bool Visible {
			get { return m_Visible; }
			set { InternalVisible = m_Visible = value; }
		}
		
		private bool m_InternalVisible = true;
		protected bool InternalVisible {
			get { return m_InternalVisible; }
			set {
				if( m_InternalVisible != value )
					API.Externals.ShowCursor(m_InternalVisible = value);
			}
		}
		
		protected bool m_Clipped = false;
		public bool Clipped {
			get { return m_Clipped; }
			set {
				if( m_Clipped != value ) {
					m_Clipped = value;
					if( m_Window != null ) {
						if( m_Clipped ) {
							Rectangle rect = m_Window.GetClientRect();
							RECT clipRect = new RECT();
							clipRect.left = rect.Left;
							clipRect.top = rect.Top;
							clipRect.bottom = rect.Bottom;
							clipRect.right = rect.Right;
							API.Externals.ClipCursor(ref clipRect);
						}
						else {
							API.Externals.ClipCursor(IntPtr.Zero);
						}
					}
				}
			}
		}
		
		protected bool m_InfinityMode = true;
		public bool InfinityMode {
			get { return m_InfinityMode; }
			set { m_InfinityMode = value; }
		}
		
		private Rectangle m_ClientRect = Rectangle.Zero;
		private bool m_Sizing = false;
		
		protected int m_X, m_Y, m_W, m_B, m_CurW, m_CurB;
		protected int m_PrevX, m_PrevY, m_PrevW, m_PrevB;
		protected int m_DeltaX, m_DeltaY, m_DeltaW;
		protected MouseButton m_DeltaB;
		
		public int X { get { return m_X; } }
		public int Y { get { return m_Y; } }
		public int Wheel { get { return m_W; } }
		public MouseButton Buttons { get { return (MouseButton)m_B; } }

		public int PrevX { get { return m_PrevX; } }
		public int PrevY { get { return m_PrevY; } }
		public int PrevWheel { get { return m_PrevW; } }
		public MouseButton PrevButtons { get { return (MouseButton)m_PrevB; } }

		public int DeltaX { get { return m_DeltaX; } }
		public int DeltaY { get { return m_DeltaY; } }
		public int DeltaWheel { get { return m_DeltaW; } }
		public MouseButton ChangedButtons { get { return m_DeltaB; } }

		public bool LeftButtonDown { get { return (m_B & (int)MouseButton.Left) == (int)MouseButton.Left; } }
		public bool RightButtonDown { get { return (m_B & (int)MouseButton.Right) == (int)MouseButton.Right; } }
		public bool MiddleButtonDown { get { return (m_B & (int)MouseButton.Middle) == (int)MouseButton.Middle; } }

		public bool LeftButtonUp { get { return (m_B & (int)MouseButton.Left) != (int)MouseButton.Left; } }
		public bool RightButtonUp { get { return (m_B & (int)MouseButton.Right) != (int)MouseButton.Right; } }
		public bool MiddleButtonUp { get { return (m_B & (int)MouseButton.Middle) != (int)MouseButton.Middle; } }
		
		public bool LeftButtonWasDown { get { return (m_PrevB & (int)MouseButton.Left) == (int)MouseButton.Left; } }
		public bool RightButtonWasDown { get { return (m_PrevB & (int)MouseButton.Right) == (int)MouseButton.Right; } }
		public bool MiddleButtonWasDown { get { return (m_PrevB & (int)MouseButton.Middle) == (int)MouseButton.Middle; } }

		public bool LeftButtonWasUp { get { return (m_PrevB & (int)MouseButton.Left) != (int)MouseButton.Left; } }
		public bool RightButtonWasUp { get { return (m_PrevB & (int)MouseButton.Right) != (int)MouseButton.Right; } }
		public bool MiddleButtonWasUp { get { return (m_PrevB & (int)MouseButton.Middle) != (int)MouseButton.Middle; } }

		protected Point2 m_PrevMousePos;
		protected Point2 m_MousePos;
		
		public Point2 NativePosition { get { return m_MousePos; } }
		
		public virtual void SetWindow(Win32NativeWindow window) {
			if( m_Window == window )
				return;
			if( m_Window != null ) {
				m_Window.CloseEvent -= OnWindowClose;
				m_Window.ResizeEvent -= OnWindowResize;
				m_Window.MoveEvent -= OnWindowMove;
				m_Window.EnterSizeMoveEvent -= OnWindowEnterSizeMove;
				m_Window.ExitSizeMoveEvent -= OnWindowExitSizeMove;
			}
			
			m_Window = window;
			
			if( m_Window != null && !m_Window.Disposed ) {
				API.Externals.GetCursorPos(ref m_PrevMousePos);
				
				m_ClientRect = window.GetClientRect(); // this only gives width and height
				Point2 point = Point2.Zero;
				API.Externals.ClientToScreen(window.Handle, ref point);
				m_ClientRect.X = point.X;
				m_ClientRect.Y = point.Y;
				
				m_X = m_PrevX = m_PrevMousePos.X - m_ClientRect.X;
				m_Y = m_PrevY = m_PrevMousePos.Y - m_ClientRect.Y;
				
				m_Window.CloseEvent += OnWindowClose;
 				m_Window.ResizeEvent += OnWindowResize;
				m_Window.MoveEvent += OnWindowMove;
				m_Window.EnterSizeMoveEvent += OnWindowEnterSizeMove;
				m_Window.ExitSizeMoveEvent += OnWindowExitSizeMove;
			}
		}
		
		protected virtual void Reset() {
			m_X = m_Y = m_W = m_B = m_CurB = m_CurW = 0;
			m_PrevX = m_PrevY = m_PrevW = m_PrevB = 0;
			m_DeltaX = m_DeltaY = m_DeltaW = 0;
			m_DeltaB = MouseButton.None;
			m_MousePos = new Point2(0, 0);
			m_PrevMousePos = new Point2(0, 0);
		}
		
		public virtual void Dispose() {
			SetWindow(null);
			
			Application.PreIdleEvent -= OnBeforeFrame;
			Application.ActivateAppEvent -= OnAppActivate;
			Application.DeactivateAppEvent -= OnAppDeactivate;
			Win32Application.ProcessWindowMessageEvent -= OnWndProc;
		}
		
		private void OnWndProc(WindowMessageEventArgs args) {
			if( args.PreventsDefault )
				return;
			switch( args.uMsg ) {
				case WindowMessageEnum.LBUTTONDOWN: {
					OnMouseDown((int)MouseButton.Left);
					args.ReturnValue = (IntPtr)1;
					args.PreventDefault();
					break;
				}
				case WindowMessageEnum.LBUTTONUP: {
					OnMouseUp((int)MouseButton.Left);
					args.ReturnValue = (IntPtr)1;
					args.PreventDefault();
					break;
				}
				case WindowMessageEnum.RBUTTONDOWN: {
					OnMouseDown((int)MouseButton.Right);
					args.ReturnValue = (IntPtr)1;
					args.PreventDefault();
					break;
				}
				case WindowMessageEnum.RBUTTONUP: {
					OnMouseUp((int)MouseButton.Right);
					args.ReturnValue = (IntPtr)1;
					args.PreventDefault();
					break;
				}
				case WindowMessageEnum.MBUTTONDOWN: {
					OnMouseDown((int)MouseButton.Middle);
					args.ReturnValue = (IntPtr)1;
					args.PreventDefault();
					break;
				}
				case WindowMessageEnum.MBUTTONUP: {
					OnMouseUp((int)MouseButton.Middle);
					args.ReturnValue = (IntPtr)1;
					args.PreventDefault();
					break;
				}
				case WindowMessageEnum.MOUSEWHEEL: {
					OnMouseWheel(unchecked((short)(((uint)args.wParam & 0xFFFF0000) >> 16) / 120));
					args.ReturnValue = (IntPtr)1;
					args.PreventDefault();
					break;
				}
			}
		}
		
		protected virtual void OnWindowEnterSizeMove(SizingAndMovingStateChangeEventArgs args) {
			m_Sizing = true;
		}
		
		protected virtual void OnWindowExitSizeMove(SizingAndMovingStateChangeEventArgs args) {
			m_Sizing = false;
			
			if( !m_Visible ) {
				int x, y;
				x = m_ClientRect.X + m_ClientRect.Width / 2;
				y = m_ClientRect.Y + m_ClientRect.Height / 2;
				InternalVisible = false;
				API.Externals.SetCursorPos(x, y);
				m_PrevMousePos = new Point2(x, y);
			}
			
			if( m_Clipped ) {
				RECT clipRect = new RECT();
				clipRect.left = m_ClientRect.Left;
				clipRect.top = m_ClientRect.Top;
				clipRect.bottom = m_ClientRect.Bottom;
				clipRect.right = m_ClientRect.Right;
				API.Externals.ClipCursor(ref clipRect);
			}
		}
		
		protected virtual void OnWindowMove(MoveEventArgs args) {
			int deltaX = args.NewPosition.X - m_ClientRect.X;
			int deltaY = args.NewPosition.Y - m_ClientRect.Y;
			
			m_ClientRect.X = args.NewPosition.X;
			m_ClientRect.Y = args.NewPosition.Y;
			
			m_X -= deltaX;
			m_Y -= deltaY;
		}
		
		protected virtual void OnWindowResize(ResizeEventArgs args) {
			// int deltaW = size.Width - m_ClientRect.Width;
			int deltaH = args.NewSize.Height - m_ClientRect.Height;
			
			m_ClientRect.Width = args.NewSize.Width;
			m_ClientRect.Height = args.NewSize.Height;
		}
		
		protected virtual void OnWindowClose(CloseEventArgs args) {
			OnAppDeactivate();
			SetWindow(null);
		}
		
		protected virtual void OnMouseDown(int button) { m_CurB |= button; }
		protected virtual void OnMouseUp(int button) { m_CurB &= ~button; }
		protected virtual void OnMouseWheel(int delta) { m_CurW += delta; }
		
		protected virtual void OnBeforeFrame() {
			API.Externals.GetCursorPos(ref m_MousePos);
			
			m_DeltaX = m_MousePos.X - m_PrevMousePos.X;
			m_DeltaY =  m_MousePos.Y - m_PrevMousePos.Y;
			m_DeltaW = m_CurW - m_W;
			m_DeltaB = (MouseButton)(m_CurB ^ m_PrevB);
			
			m_PrevX = m_X;
			m_PrevY = m_Y;
			m_PrevW = m_W;
			m_PrevB = m_B;
			
			m_X += m_DeltaX;
			m_Y += m_DeltaY;
			m_W = m_CurW;
			m_B = m_CurB;

			if( m_Visible || m_Sizing || !Application.Active || m_Window == null )
				m_PrevMousePos = m_MousePos;
			else {
				int x, y;
				
				x = m_ClientRect.X + m_ClientRect.Width / 2;
				y = m_ClientRect.Y + m_ClientRect.Height / 2;
				if( m_DeltaX != 0 || m_DeltaY != 0 )
					API.Externals.SetCursorPos(x, y);
				m_PrevMousePos = new Point2(x, y);
				
				if( m_X < 0 ) m_X = 0;
				if( m_Y < 0 ) m_Y = 0;
				if( m_X >= m_ClientRect.Width ) m_X = m_ClientRect.Width - 1;
				if( m_Y >= m_ClientRect.Height ) m_Y = m_ClientRect.Height - 1;
			}
		}
		
		protected virtual void OnAppActivate() {
			if( m_Window != null && m_X >= 0 && m_Y >= 0 && m_X < m_ClientRect.Width && m_Y < m_ClientRect.Height ) {
				if( !m_Visible ) {
					int x, y;
					x = m_ClientRect.X + m_ClientRect.Width / 2;
					y = m_ClientRect.Y + m_ClientRect.Height / 2;
					InternalVisible = false;
					API.Externals.SetCursorPos(x, y);
					m_PrevMousePos = new Point2(x, y);
				}
				
				if( m_Clipped ) {
					RECT clipRect = new RECT();
					clipRect.left = m_ClientRect.Left;
					clipRect.top = m_ClientRect.Top;
					clipRect.bottom = m_ClientRect.Bottom;
					clipRect.right = m_ClientRect.Right;
					API.Externals.ClipCursor(ref clipRect);
				}
			}
		}
		
		protected virtual void OnAppDeactivate() {
			if( m_Window != null ) {
				if( !m_Visible ) {
					int x, y;
					x = m_ClientRect.Left + m_X;
					y = m_ClientRect.Top + m_Y;
					API.Externals.SetCursorPos(x, y);
					InternalVisible = true;
					m_PrevMousePos = new Point2(x, y);
				}
				
				if( m_Clipped ) {
					API.Externals.ClipCursor(IntPtr.Zero);
				}
			}
		}
	}
}
