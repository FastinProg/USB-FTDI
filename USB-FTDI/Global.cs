using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace USB_FTDI
{
	public static class Global
	{
		public static Stack<Control> BackHistory = new Stack<Control>();
		public static UIElement castomBckHistore = new UIElement();
		public static Grid MainControl;


		public static void NavigateTo(Control from, Control to)
		{
			Global.MainControl.Children.Clear();
			Global.MainControl.Children.Add(to);
			Global.BackHistory.Push(from);
		}
		public static void castomNavigateTo(UIElement from, UIElement to)
		{
			Global.MainControl.Children.Clear();
			Global.MainControl.Children.Add(to);
			Global.castomBckHistore = from;
		}
		public static void CastomNavigateBack()
		{

			Global.MainControl.Children.Clear();
			Global.MainControl.Children.Add(Global.castomBckHistore);

		}

		public static void NavigateBack()
		{
			if (Global.BackHistory.Count > 0)
			{
				Global.MainControl.Children.Clear();
				Global.MainControl.Children.Add(Global.BackHistory.Pop());
			}
		}
	}


	public enum NotifyLevel { None, All, Warning, Error }
}
