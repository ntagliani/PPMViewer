using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Path = System.IO.Path;

namespace PPMViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private FileSystemWatcher? m_watcher;
        private string? m_filePath;
        public MainWindow()
        {
            InitializeComponent();
            this.Drop += MyImage_OnDropEvent;

            string[] args = Environment.GetCommandLineArgs();
            if (args.Length > 1 && InitializeWatcher(args[1]))
            {
                UpdateImage();
            }
            else
            {
                StatusBar.Content = "Drop a PPM image in here to start monitoring it";
            }
        }

        private void MyImage_OnDropEvent(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
                return;

            // Note that you can have more than one file.
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length != 1)
                return;

            if (InitializeWatcher(files[0]))
            {
                UpdateImage();
            }
            e.Handled = true;
        }

        private void UpdateImage()
        {
            if (m_filePath == null) return;

            string[] lines;
            try
            {
                lines = File.ReadAllLines(m_filePath);
            }
            catch (System.IO.IOException)
            {
                return;
            }
            if (lines.Length < 3) return;
            if (!lines[0].StartsWith("P3")) return;
            string[] imageSize = lines[1].Trim().Split(' ');
            if (imageSize.Length != 2) return;

            int width = Int32.Parse(imageSize[0]);
            int height = Int32.Parse(imageSize[1]);
            if (lines.Length != ((width * height) + 3)) return;
            // Define parameters used to create the BitmapSource.
            PixelFormat pf = PixelFormats.Bgr32;
            int rawStride = (width * pf.BitsPerPixel + 7) / 8;
            byte[] rawImage = new byte[rawStride * height];

            for (int l = 3; l < lines.Length; l++)
            {
                string[] pixel = lines[l].Trim().Split(' ');
                int firstPixelIndex = (l - 3) * 4;
                rawImage[firstPixelIndex] = Byte.Parse(pixel[2]);
                rawImage[firstPixelIndex + 1] = Byte.Parse(pixel[1]);
                rawImage[firstPixelIndex + 2] = Byte.Parse(pixel[0]);
            }

            // Create a BitmapSource.
            BitmapSource bitmap = BitmapSource.Create(width, height,
                96, 96, pf, null,
                rawImage, rawStride);

            MyImage.Source = bitmap;
            StatusBar.Content = $"{m_filePath} Last update {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}";
        }
        private bool InitializeWatcher(string path)
        {
            if (!File.Exists(path))
            {
                return false;
            }
            var folder = Path.GetDirectoryName(path);
            if (folder == null)
            {
                return false;
            }

            var extension = Path.GetExtension(path);
            m_watcher = new FileSystemWatcher(folder);

            m_watcher.NotifyFilter = NotifyFilters.Attributes
                                 | NotifyFilters.CreationTime
                                 | NotifyFilters.DirectoryName
                                 | NotifyFilters.FileName
                                 | NotifyFilters.LastAccess
                                 | NotifyFilters.LastWrite
                                 | NotifyFilters.Security
                                 | NotifyFilters.Size;

            m_watcher.Changed += OnChanged;
            m_watcher.Created += OnCreated;
            m_watcher.Deleted += OnDeleted;
            m_watcher.Renamed += OnRenamed;
            m_watcher.Error += OnError;

            if (extension != null)
            {
                m_watcher.Filter = $"*{extension}";

            }
            m_watcher.IncludeSubdirectories = false;
            m_watcher.EnableRaisingEvents = true;

            m_filePath = path;

            return true;
        }

        private void OnError(object sender, ErrorEventArgs e)
        {
            // ignore
        }

        private void OnRenamed(object sender, RenamedEventArgs e)
        {
            // ignore
        }

        private void OnDeleted(object sender, FileSystemEventArgs e)
        {
            // ignore 
        }

        private void OnCreated(object sender, FileSystemEventArgs e)
        {
            if (m_filePath == null || Path.GetFullPath(e.FullPath) != Path.GetFullPath(m_filePath))
                return;
            Application.Current.Dispatcher.Invoke(
            () =>
            {
                // Code to run on the GUI thread.
                UpdateImage();
            });

        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            if (m_filePath == null || Path.GetFullPath(e.FullPath) != Path.GetFullPath(m_filePath))
                return;
            Application.Current.Dispatcher.Invoke(
            () =>
            {
                // Code to run on the GUI thread.
                UpdateImage();
            });
        }
    }
}
