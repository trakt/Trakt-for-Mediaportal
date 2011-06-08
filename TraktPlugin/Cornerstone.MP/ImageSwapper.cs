using System;
using System.Collections.Generic;
using System.Text;
using MediaPortal.GUI.Library;
using System.Threading;

namespace TraktPlugin.GUI
{
    /// <summary>
    /// This class takes two GUIImage objects so that you can treat them as one. When you assign
    /// a new image to this object using the Filename property, the currently active image is 
    /// hidden and the second is made visibile (with the new image file displayed). This allows
    /// for animations on image change, such as a fading transition.
    /// 
    /// This class also uses the AsyncImageResource class for asynchronus image loading, 
    /// dramtically improving GUI performance. It also takes advantage of the Delay feature of
    /// the AsyncImageResource to prevent unneccisary loads when rapid image changes are made.
    /// </summary>
    public class ImageSwapper
    {
        private bool imagesNeedSwapping = false;
        private object loadingLock = new object();

        /// <summary>
        /// Image loading only occurs when set to true. If false all resources will be unloaded
        /// and all GUIImage objects set to invisible. Setting Active to false also clears the
        /// Filename property.
        /// </summary>
        public bool Active
        {
            get { return _active; }
            set
            {
                if (_active == value)
                    return;

                _active = value;
                _imageResource.Active = _active;

                // if we are inactive be sure both properties are cleared
                if (!Active)
                {
                    _imageResource.Property = _propertyTwo;
                    _imageResource.Property = _propertyOne;
                }
            }
        }
        private bool _active = true;

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
                lock (loadingLock)
                {
                    if (!Active)
                        value = null;

                    if ((value != null && value.Equals(_filename)) || _guiImageOne == null)
                        return;

                    // if we have a second backdrop image object, alternate between the two
                    if (_guiImageTwo != null && imagesNeedSwapping)
                    {
                        if (_imageResource.Property.Equals(_propertyOne))
                            _imageResource.Property = _propertyTwo;
                        else
                            _imageResource.Property = _propertyOne;

                        imagesNeedSwapping = false;
                    }

                    // update resource with new file
                    _filename = value;
                    if (_loadingImage != null) _loadingImage.Visible = true;
                    _imageResource.Filename = _filename;
                }
            }
        }
        string _filename = null;

        /// <summary>
        /// First GUIImage used for the visibilty toggle behavior. If set to NULL the ImageSwapper
        /// behaves as if inactive.
        /// </summary>
        public GUIImage GUIImageOne
        {
            get { return _guiImageOne; }
            set
            {
                if (_guiImageOne == value)
                    return;

                _guiImageOne = value;
                if (_guiImageOne != null)
                {
                    _guiImageOne.FileName = _propertyOne;
                    _filename = null;
                }
            }
        }
        private GUIImage _guiImageOne;

        /// <summary>
        /// Second GUIImage used for the visibility toggle behavior. If set to NULL no toggling
        /// occurs and only GUIImageOne is used. This provides backwards compatibility if a skin
        /// does not implement the second GUIImage control.
        /// </summary>
        public GUIImage GUIImageTwo
        {
            get { return _guiImageTwo; }
            set
            {
                if (_guiImageTwo == value)
                    return;

                _guiImageTwo = value;
                if (_guiImageTwo != null)
                {
                    _guiImageTwo.FileName = _propertyTwo;
                    _filename = null;
                }
            }
        }
        private GUIImage _guiImageTwo;

        /// <summary>
        /// If set, this image object will be set to visible during the load process and will
        /// be set to hidden when the next image has completed loading.
        /// </summary>
        public GUIImage LoadingImage
        {
            get { return _loadingImage; }
            set
            {
                _loadingImage = value;
            }
        } private GUIImage _loadingImage;

        /// <summary>
        /// The property assigned to the first GUIImage. Assigning this property to the texture
        /// field of another GUIImage object will result in the image being loaded there. This
        /// can also be useful for backwards compatibility.
        /// </summary>
        public string PropertyOne
        {
            get { return _propertyOne; }
            set
            {
                if (_imageResource.Property.Equals(_propertyOne))
                    _imageResource.Property = value;

                _propertyOne = value;
            }
        }
        private string _propertyOne = "#Cornerstone.ImageSwapper1";

        /// <summary>
        /// The property field used for the second GUIImage.
        /// </summary>
        public string PropertyTwo
        {
            get { return _propertyTwo; }
            set
            {
                if (_imageResource.Property.Equals(_propertyTwo))
                    _imageResource.Property = value;

                _propertyTwo = value;
            }
        }
        private string _propertyTwo = "#Cornerstone.ImageSwapper2";

        /// <summary>
        /// The AsyncImageResource backing this object. All image loading and unloading is done
        /// in the background by this object.
        /// </summary>
        public AsyncImageResource ImageResource
        {
            get { return _imageResource; }
        }
        private AsyncImageResource _imageResource;


        public ImageSwapper()
        {
            _imageResource = new AsyncImageResource();
            _imageResource.Property = _propertyOne;
            _imageResource.ImageLoadingComplete += new AsyncImageLoadComplete(imageResource_ImageLoadingComplete);
        }

        // Once image loading is complete this method is called and the visibility of the
        // two GUIImages is swapped.
        private void imageResource_ImageLoadingComplete(AsyncImageResource image)
        {
            lock (loadingLock)
            {
                if (_guiImageOne == null)
                    return;

                if (_filename == null)
                {
                    if (_guiImageOne != null) _guiImageOne.Visible = false;
                    if (_guiImageTwo != null) _guiImageTwo.Visible = false;
                    return;
                }

                _guiImageOne.ResetAnimations();
                if (_guiImageTwo != null) _guiImageTwo.ResetAnimations();

                // if we have a second backdrop image object, alternate between the two
                if (_guiImageTwo != null)
                {
                    if (_imageResource.Property.Equals(_propertyOne))
                    {
                        _guiImageOne.Visible = _active;
                        _guiImageTwo.Visible = false;
                    }
                    else
                    {
                        _guiImageOne.Visible = false;
                        _guiImageTwo.Visible = _active;
                    }

                    imagesNeedSwapping = true;
                }


                // if no 2nd backdrop control, just update normally
                else _guiImageOne.Visible = _active;

                if (_loadingImage != null) _loadingImage.Visible = false;
            }
        }

    }
}
