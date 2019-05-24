using System;
using System.Data;
using System.Threading;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Reflection;
using MediaPortal.GUI.Library;
using System.IO;

namespace TraktPlugin.GUI
{
    public delegate void AsyncImageLoadComplete(AsyncImageResource image);

    public class AsyncImageResource
    {
        private Object loadingLock = new Object();
        private int pendingToken = 0;
        private int threadsWaiting = 0;
        private bool warned = false;


        /// <summary>
        /// This event is triggered when a new image file has been successfully loaded
        /// into memory.
        /// </summary>
        public event AsyncImageLoadComplete ImageLoadingComplete;

        /// <summary>
        /// True if this resources will actively load into memory when assigned a file.
        /// </summary>
        public bool Active
        {
            get
            {
                return _active;
            }

            set
            {
                if (_active == value)
                    return;

                _active = value;

                Thread newThread = new Thread(new ThreadStart(activeWorker));
                newThread.Name = "Cornerstone";
                newThread.Start();
            }
        }
        private bool _active = true;

        /// <summary>
        /// If multiple changes to the Filename property are made in rapid succession, this delay
        /// will be used to prevent uneccesary loading operations. Most useful for large images that
        /// take a non-trivial amount of time to load from memory.
        /// </summary>
        public int Delay
        {
            get { return _delay; }
            set { _delay = value; }
        } 
        private int _delay = 250;

        private void activeWorker()
        {
            lock (loadingLock)
            {
                if (_active)
                {
                    // load the resource
                    _identifier = loadResourceSafe(_filename);

                    // notify any listeners a resource has been loaded
                    ImageLoadingComplete?.Invoke(this);
                }
                else
                {
                    unloadResource(_filename);
                    _identifier = null;
                }
            }
        }


        /// <summary>
        /// This MediaPortal property will automatically be set with the renderable identifier
        /// once the resource has been loaded. Appropriate for a texture field of a GUIImage 
        /// control.
        /// </summary>
        public string Property
        {
            get { return _property; }
            set
            {
                _property = value;

                writeProperty();
            }
        }
        private string _property = null;

        private void writeProperty()
        {
            if (_active && _property != null && _identifier != null)
            {
                GUIPropertyManager.SetProperty(_property, _identifier);
            }
            else
            {
                if (_property != null)
                {
                    GUIPropertyManager.SetProperty(_property, "-");
                }
            }
        }


        /// <summary>
        /// The identifier used by the MediaPortal GUITextureManager to identify this resource.
        /// This changes when a new file has been assigned, if you need to know when this changes
        /// use the ImageLoadingComplete event.
        /// </summary>
        public string Identifier
        {
            get { return _identifier; }
        }
        string _identifier = null;


        /// <summary>
        /// The filename of the image backing this resource. Reassign to change textures.
        /// </summary>
        public string Filename
        {
            get
            {
                return _filename;
            }

            set
            {
                Thread newThread = new Thread(new ParameterizedThreadStart(setFilenameWorker));
                newThread.Name = "Cornerstone";
                newThread.Start(value);
            }
        }
        string _filename = null;

        // Unloads the previous file and sets a new filename. 
        private void setFilenameWorker(object newFilenameObj)
        {
            int localToken = ++pendingToken;
            string oldFilename = _filename;

            // check if another thread has locked for loading
            bool loading = Monitor.TryEnter(loadingLock);
            if (loading) Monitor.Exit(loadingLock);

            // if a loading action is in progress or another thread is waiting, we wait too
            if (loading || threadsWaiting > 0)
            {
                threadsWaiting++;
                for (int i = 0; i < 5; i++)
                {
                    Thread.Sleep(_delay / 5);
                    if (localToken < pendingToken)
                        return;
                }
                threadsWaiting--;
            }

            lock (loadingLock)
            {
                if (localToken < pendingToken)
                    return;

                // type cast and clean our filename
                string newFilename = newFilenameObj as string;
                if (newFilename != null && newFilename.Trim().Length == 0)
                    newFilename = null;
                else if (newFilename != null)
                    newFilename = newFilename.Trim();

                // if we are not active we should not be assigning a filename
                if (!Active) newFilename = null;

                // if there is no change, quit
                if (_filename != null && _filename.Equals(newFilename))
                {
                    ImageLoadingComplete?.Invoke(this);

                    return;
                }

                string newIdentifier = loadResourceSafe(newFilename);
                
                // check if we have a new loading action pending, if so just quit
                if (localToken < pendingToken)
                {
                    unloadResource(newIdentifier);
                    return;
                }
                
                // update MediaPortal about the image change                
                _identifier = newIdentifier;
                _filename = newFilename;
                writeProperty();

                // notify any listeners a resource has been loaded
                ImageLoadingComplete?.Invoke(this);
            }

            // wait a few seconds in case we want to quickly reload the previous resource
            // if it's not reassigned, unload from memory.
            Thread.Sleep(5000);
            lock (loadingLock)
            {
                if (_filename != oldFilename)
                    unloadResource(oldFilename);
            }
        }


        /// <summary>
        /// Loads the given file into memory and registers it with MediaPortal.
        /// </summary>
        /// <param name="filename">The image file to be loaded.</param>
        private bool loadResource(string filename)
        {
            if (!_active || filename == null || !File.Exists(filename))
                return false;

            try
            {
                if (GUITextureManager.Load(filename, 0, 0, 0, true) > 0)
                    return true;
            }
            catch { }
            
            return false;
        }

        private string loadResourceSafe(string filename)
        {
            if (filename == null || filename.Trim().Length == 0)
                return null;

            // try to load with new persistent load feature
            try
            {
                if (loadResource(filename))
                    return filename;
            }
            catch (MissingMethodException)
            {
                if (!warned)
                {
                    warned = true;
                }
            }

            // if not available load image ourselves and pass to MediaPortal. Much slower but this still
            // gives us asynchronous loading. 
            Image image = LoadImageFastFromFile(filename);

            if (image != null)
            {
                if (GUITextureManager.LoadFromMemory(image, getIdentifier(filename), 0, 0, 0) > 0)
                {
                    return getIdentifier(filename);
                }
            }
            return null;
        }

        private string getIdentifier(string filename)
        {
            return "[Trakt:" + filename.GetHashCode() + "]";
        }

        /// <summary>
        /// If previously loaded, unloads the resource from memory and removes it 
        /// from the MediaPortal GUITextureManager.
        /// </summary>
        private void unloadResource(string filename)
        {

            if (filename == null)
                return;

            // double duty since we dont know if we loaded via new fast way or old
            // slow way
            GUITextureManager.ReleaseTexture(getIdentifier(filename));
            GUITextureManager.ReleaseTexture(filename);
        }


        [DllImport("gdiplus.dll", CharSet = CharSet.Unicode)]
        private static extern int GdipLoadImageFromFile(string filename, out IntPtr image);

        // Loads an Image from a File by invoking GDI Plus instead of using build-in 
        // .NET methods, or falls back to Image.FromFile. GDI Plus should be faster.
        public static Image LoadImageFastFromFile(string filename)
        {
            IntPtr imagePtr = IntPtr.Zero;
            Image image = null;

            try
            {
                if (GdipLoadImageFromFile(filename, out imagePtr) != 0)
                {
                    image = LoadImageSafe(filename);
                }

                else
                    image = (Image)typeof(Bitmap).InvokeMember("FromGDIplus", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.InvokeMethod, null, null, new object[] { imagePtr });
            }
            catch (Exception e)
            {
                TraktLogger.Warning("Failed to load image {0}: {1}", filename, e.Message);
                image = null;
            }

            return image;

        }

        /// <summary>
        /// Method to safely load an image from a file without leaving the file open       
        /// </summary> 
        public static Image LoadImageSafe(string filePath)
        {
            try
            {
                FileInfo fi = new FileInfo(filePath);

                if (!fi.Exists) throw new FileNotFoundException("Cannot find image");
                if (fi.Length == 0) throw new FileNotFoundException("Zero length image file");

                // Image.FromFile is known to leave files open, so we use a stream instead to read it
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    if (!fs.CanRead) throw new FileLoadException("Cannot read file stream");

                    if (fs.Length == 0) throw new FileLoadException("File stream zero length");

                    using (Image original = Image.FromStream(fs))
                    {
                        // Make a copy of the file in memory, then release the one GDI+ gave us
                        // thus ensuring that all file handles are closed properly (which GDI+ doesn’t do for us in a timely fashion)
                        int width = original.Width;
                        int height = original.Height;
                        if (width == 0) throw new DataException("Bad image dimension width=0");
                        if (height == 0) throw new DataException("Bad image dimension height=0");

                        Bitmap copy = new Bitmap(width, height);
                        using (Graphics graphics = Graphics.FromImage(copy))
                        {
                            graphics.DrawImage(original, 0, 0, copy.Width, copy.Height);
                        }
                        return copy;
                    }
                }
            }
            catch (Exception ex)
            {
                ex.Data.Add("FileName", filePath);
                throw;
            }
        }
    }
}