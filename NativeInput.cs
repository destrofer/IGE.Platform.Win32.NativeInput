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
using System.Reflection;
using IGE.Input;

namespace IGE.Platform.Win32 {
	/// <summary>
	/// </summary>
	public sealed class NativeInput : IInputDriver {
		public string DriverName { get { return "Native Win32 mouse and keyboard input"; } }
		public Version DriverVersion { get { return Assembly.GetExecutingAssembly().GetName().Version; } }
		public bool IsSupported { get { return true; } }

		internal static IInputDevice[] m_Devices = null;
		internal static IKeyboardDevice[] m_KeyboardDevices = null;
		internal static IMouseDevice[] m_MouseDevices = null;
		internal static IControllerDevice[] m_ControllerDevices = null;
		internal static IPenDevice[] m_PenDevices = null;
		internal static ITouchDevice[] m_TouchDevices = null;
		
		/// <summary>
		/// Contains the singleton instance of this class.
		/// </summary>
		internal static NativeInput Instance = null;
		public static IInputDriver GetInstance() {
			if( Instance != null )
				return Instance;
			return Instance = new NativeInput();
		}

		private NativeInput() {
		}
		
		public bool Initialize() {
			RescanDevices();
			return true;
		}
		
		public bool Test() {
			return true;
		}
		
		public void RescanDevices() {
			m_KeyboardDevices = new IKeyboardDevice[] { KeyboardDevice.GetInstance() };
			m_MouseDevices = new IMouseDevice[] { MouseDevice.GetInstance() };
			m_ControllerDevices = new IControllerDevice[] { };
			m_PenDevices = new IPenDevice[] { };
			m_TouchDevices = new ITouchDevice[] { };
			
			List<IInputDevice> devices = new List<IInputDevice>();
			devices.AddRange(m_KeyboardDevices);
			devices.AddRange(m_MouseDevices);
			devices.AddRange(m_ControllerDevices);
			devices.AddRange(m_PenDevices);
			devices.AddRange(m_TouchDevices);
			m_Devices = devices.ToArray();
		}
		
		public IInputDevice[] InputDevices { get { return m_Devices; } }
		public IKeyboardDevice[] KeyboardDevices { get { return m_KeyboardDevices; } }
		public IMouseDevice[] MouseDevices { get { return m_MouseDevices; } }
		public IControllerDevice[] ControllerDevices { get { return m_ControllerDevices; } }
		public IPenDevice[] PenDevices { get { return m_PenDevices; } }
		public ITouchDevice[] TouchDevices { get { return m_TouchDevices; } }
	}
}
