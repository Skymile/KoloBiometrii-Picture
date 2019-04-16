using System.IO;
using Microsoft.Win32;

namespace Paint
{
	public static class Dialogs
	{
		public static bool TryLoad(out string filename)
		{
			if (open.ShowDialog() == true)
			{
				filename = open.FileName;
				return true;
			}
			filename = null;
			return false;
		}

		public static bool TrySave(out string filename)
		{
			if (save.ShowDialog() == true)
			{
				filename = save.FileName;
				return true;
			}
			filename = null;
			return false;
		}

		private static readonly OpenFileDialog open = new OpenFileDialog
		{
			Title = "Wybierz obraz",
			InitialDirectory = Directory.GetCurrentDirectory()
		};

		private static readonly SaveFileDialog save = new SaveFileDialog
		{
			Title = "Wybierz obraz",
			InitialDirectory = Directory.GetCurrentDirectory()
		};
	}
}
