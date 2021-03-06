﻿using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Globalization;
using System.ComponentModel;

using System.Windows;
using System.Windows.Media;
using System.Windows.Markup;
using System.Windows.Controls;
using System.Windows.Resources;

using SharpVectors.Runtime;
using SharpVectors.Renderers.Wpf;

namespace SharpVectors.Converters
{
    /// <summary>
    /// This is a <see cref="Canvas"/> control for viewing SVG file in WPF
    /// applications.
    /// </summary>
    /// <remarks>
    /// It extends the drawing canvas, <see cref="SvgDrawingCanvas"/>, instead of
    /// generic <see cref="Canvas"/> control, therefore any interactivity support 
    /// implemented in the drawing canvas will be available in the 
    /// <see cref="Canvas"/>.
    /// </remarks>
    public class SvgCanvas : SvgDrawingCanvas, IUriContext
    {
        #region Public Fields

        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register("Source", typeof(Uri), typeof(SvgCanvas),
                new FrameworkPropertyMetadata(null, OnSourceChanged));

        #endregion

        #region Private Fields

        private bool _isAutoSized;
        private bool _autoSize;
        private bool _textAsGeometry;
        private bool _includeRuntime;
        private bool _optimizePath;

        private DrawingGroup _svgDrawing;

        private CultureInfo _culture;

        private Uri _baseUri;
        private Uri _sourceUri;

        #endregion

        #region Constructors and Destructor

        /// <summary>
        /// Initializes a new instance of the <see cref="SvgCanvas"/> class.
        /// </summary>
        public SvgCanvas()
        {
            _textAsGeometry = false;
            _includeRuntime = true;
            _optimizePath   = true;
        }

        /// <summary>
        /// Static constructor to define metadata for the control (and link it to the style in Generic.xaml).
        /// </summary>
        static SvgCanvas()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SvgCanvas),
                new FrameworkPropertyMetadata(typeof(SvgCanvas)));
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets the path to the SVG file to load into this 
        /// <see cref="Canvas"/>.
        /// </summary>
        /// <value>
        /// A <see cref="System.Uri"/> specifying the path to the SVG source file.
        /// The file can be located on a computer, network or assembly resources.
        /// Settings this to <see langword="null"/> will close any opened diagram.
        /// </value>
        public Uri Source
        {
            get {
                return (Uri)GetValue(SourceProperty);
            }
            set {
                _sourceUri = value;
                SetValue(SourceProperty, value);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to automatically resize this
        /// <see cref="Canvas"/> based on the size of the loaded drawing.
        /// </summary>
        /// <value>
        /// This is <see langword="true"/> if this <see cref="Canvas"/> is
        /// automatically resized based on the size of the loaded drawing;
        /// otherwise, it is <see langword="false"/>. The default is 
        /// <see langword="false"/>, and the user-defined size or the parent assigned
        /// layout size is used.
        /// </value>
        public bool AutoSize
        {
            get {
                return _autoSize;
            }
            set {
                _autoSize = value;

                this.OnAutoSizeChanged();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the path geometry is 
        /// optimized using the <see cref="StreamGeometry"/>.
        /// </summary>
        /// <value>
        /// This is <see langword="true"/> if the path geometry is optimized
        /// using the <see cref="StreamGeometry"/>; otherwise, it is 
        /// <see langword="false"/>. The default is <see langword="true"/>.
        /// </value>
        public bool OptimizePath
        {
            get {
                return _optimizePath;
            }
            set {
                _optimizePath = value;

                this.OnSettingsChanged();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the texts are rendered as
        /// path geometry.
        /// </summary>
        /// <value>
        /// This is <see langword="true"/> if texts are rendered as path 
        /// geometries; otherwise, this is <see langword="false"/>. The default
        /// is <see langword="false"/>.
        /// </value>
        public bool TextAsGeometry
        {
            get {
                return _textAsGeometry;
            }
            set {
                _textAsGeometry = value;

                this.OnSettingsChanged();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the <c>SharpVectors.Runtime.dll</c>
        /// classes are used in the generated output.
        /// </summary>
        /// <value>
        /// This is <see langword="true"/> if the <c>SharpVectors.Runtime.dll</c>
        /// classes and types are used in the generated output; otherwise, it is 
        /// <see langword="false"/>. The default is <see langword="true"/>.
        /// </value>
        /// <remarks>
        /// The use of the <c>SharpVectors.Runtime.dll</c> prevents the hard-coded
        /// font path generated by the <see cref="FormattedText"/> class, support
        /// for embedded images etc.
        /// </remarks>
        public bool IncludeRuntime
        {
            get {
                return _includeRuntime;
            }
            set {
                _includeRuntime = value;

                this.OnSettingsChanged();
            }
        }

        /// <summary>
        /// Gets or sets the main culture information used for rendering texts.
        /// </summary>
        /// <value>
        /// An instance of the <see cref="CultureInfo"/> specifying the main
        /// culture information for texts. The default is the English culture.
        /// </value>
        /// <remarks>
        /// <para>
        /// This is the culture information passed to the <see cref="FormattedText"/>
        /// class instance for the text rendering.
        /// </para>
        /// <para>
        /// The library does not currently provide any means of splitting texts
        /// into its multi-language parts.
        /// </para>
        /// </remarks>
        public CultureInfo CultureInfo
        {
            get {
                return _culture;
            }
            set {
                if (value != null)
                {
                    _culture = value;

                    this.OnSettingsChanged();
                }
            }
        }

        /// <summary>
        /// Gets the drawing from the SVG file conversion.
        /// </summary>
        /// <value>
        /// An instance of the <see cref="DrawingGroup"/> specifying the converted drawings
        /// which is rendered in this canvas.
        public DrawingGroup Drawings
        {
            get {
                return _svgDrawing;
            }
        }

        #endregion

        #region Public Methods

        #endregion

        #region Protected Methods

        /// <summary>
        /// Raises the Initialized event. This method is invoked whenever IsInitialized is set to true.
        /// </summary>
        /// <param name="e">Event data for the event.</param>
        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            if (_sourceUri != null)
            {
                if (_svgDrawing == null)
                {
                    DrawingGroup drawing = this.CreateDrawing();
                    if (drawing != null)
                    {
                        this.OnLoadDrawing(drawing);
                    }
                }
            }
        }

        /// <summary>
        /// This handles changes in the rendering settings of this control.
        /// </summary>
        protected virtual void OnSettingsChanged()
        {
            if (!this.IsInitialized || _sourceUri == null)
            {
                return;
            }

            DrawingGroup drawing = this.CreateDrawing();
            if (drawing != null)
            {
                this.OnLoadDrawing(drawing);
            }
        }

        /// <summary>
        /// This handles changes in the automatic resizing property of this control.
        /// </summary>
        protected virtual void OnAutoSizeChanged()
        {
            if (_autoSize)
            {
                if (this.IsInitialized && _svgDrawing != null)
                {
                    Rect rectDrawing = _svgDrawing.Bounds;
                    if (!rectDrawing.IsEmpty)
                    {
                        this.Width = rectDrawing.Width;
                        this.Height = rectDrawing.Height;

                        _isAutoSized = true;
                    }
                }
            }
            else
            {
                if (_isAutoSized)
                {
                    this.Width = Double.NaN;
                    this.Height = Double.NaN;
                }
            }
        }

        /// <summary>
        /// Performs the conversion of a valid SVG source file to the 
        /// <see cref="DrawingGroup"/>.
        /// </summary>
        /// <returns>
        /// This returns <see cref="DrawingGroup"/> if successful; otherwise, it
        /// returns <see langword="null"/>.
        /// </returns>
        protected virtual DrawingGroup CreateDrawing()
        {
            Uri svgSource = this.GetAbsoluteUri();

            DrawingGroup drawing = null;

            if (svgSource == null)
            {
                return drawing;
            }

            try
            {
                string scheme = svgSource.Scheme;
                if (string.IsNullOrWhiteSpace(scheme))
                {
                    return null;
                }

                WpfDrawingSettings settings = new WpfDrawingSettings();
                settings.IncludeRuntime = _includeRuntime;
                settings.TextAsGeometry = _textAsGeometry;
                settings.OptimizePath = _optimizePath;
                if (_culture != null)
                {
                    settings.CultureInfo = _culture;
                }

                switch (scheme)
                {
                    case "file":
                    //case "ftp":
                    case "https":
                    case "http":
                        using (FileSvgReader reader = new FileSvgReader(settings))
                        {
                            drawing = reader.Read(svgSource);
                        }
                        break;
                    case "pack":
                        StreamResourceInfo svgStreamInfo = null;
                        if (svgSource.ToString().IndexOf("siteoforigin",
                            StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            svgStreamInfo = Application.GetRemoteStream(svgSource);
                        }
                        else
                        {
                            svgStreamInfo = Application.GetResourceStream(svgSource);
                        }

                        Stream svgStream = (svgStreamInfo != null) ? svgStreamInfo.Stream : null;

                        if (svgStream != null)
                        {
                            string fileExt = Path.GetExtension(svgSource.ToString());
                            bool isCompressed = !string.IsNullOrWhiteSpace(fileExt) &&
                                string.Equals(fileExt, ".svgz",
                                StringComparison.OrdinalIgnoreCase);

                            if (isCompressed)
                            {
                                using (svgStream)
                                {
                                    using (GZipStream zipStream =
                                        new GZipStream(svgStream, CompressionMode.Decompress))
                                    {
                                        using (FileSvgReader reader = new FileSvgReader(settings))
                                        {
                                            drawing = reader.Read(zipStream);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                using (svgStream)
                                {
                                    using (FileSvgReader reader = new FileSvgReader(settings))
                                    {
                                        drawing = reader.Read(svgStream);
                                    }
                                }
                            }
                        }
                        break;
                    case "data":
                        var sourceData = svgSource.OriginalString.Replace(" ", "");

                        int nColon = sourceData.IndexOf(":", StringComparison.OrdinalIgnoreCase);
                        int nSemiColon = sourceData.IndexOf(";", StringComparison.OrdinalIgnoreCase);
                        int nComma = sourceData.IndexOf(",", StringComparison.OrdinalIgnoreCase);

                        string sMimeType = sourceData.Substring(nColon + 1, nSemiColon - nColon - 1);
                        string sEncoding = sourceData.Substring(nSemiColon + 1, nComma - nSemiColon - 1);

                        if (string.Equals(sMimeType.Trim(), "image/svg+xml", StringComparison.OrdinalIgnoreCase)
                            && string.Equals(sEncoding.Trim(), "base64", StringComparison.OrdinalIgnoreCase))
                        {
                            string sContent = SvgObject.RemoveWhitespace(sourceData.Substring(nComma + 1));
                            byte[] imageBytes = Convert.FromBase64CharArray(sContent.ToCharArray(),
                                0, sContent.Length);
                            using (var stream = new MemoryStream(imageBytes))
                            {
                                using (var reader = new FileSvgReader(settings))
                                {
                                    drawing = reader.Read(stream);
                                }
                            }
                        }
                        break;
                }

            }
            catch
            {
                if (DesignerProperties.GetIsInDesignMode(new DependencyObject()) ||
                    LicenseManager.UsageMode == LicenseUsageMode.Designtime)
                {
                    return drawing;
                }

                throw;
            }

            return drawing;
        }

        #endregion

        #region Private Methods

        private void OnLoadDrawing(DrawingGroup drawing)
        {
            if (drawing == null)
            {
                return;
            }

            this.OnUnloadDiagram();

            this.RenderDiagrams(drawing);

            _svgDrawing = drawing;

            this.OnAutoSizeChanged();
        }

        private void OnUnloadDiagram()
        {
            this.UnloadDiagrams();

            if (_isAutoSized)
            {
                this.Width = Double.NaN;
                this.Height = Double.NaN;
            }
        }

        private Uri GetAbsoluteUri()
        {
            if (_sourceUri == null)
            {
                return null;
            }
            Uri svgSource = _sourceUri;

            if (svgSource.IsAbsoluteUri)
            {
                return svgSource;
            }
            else
            {
                // Try getting a local file in the same directory....
                string svgPath = svgSource.ToString();
                if (svgPath[0] == '\\' || svgPath[0] == '/')
                {
                    svgPath = svgPath.Substring(1);
                }
                svgPath = svgPath.Replace('/', '\\');

                Assembly assembly = Assembly.GetExecutingAssembly();
                string localFile = Path.Combine(Path.GetDirectoryName(
                    assembly.Location), svgPath);

                if (File.Exists(localFile))
                {
                    return new Uri(localFile);
                }

                // Try getting it as resource file...
                if (_baseUri != null)
                {
                    return new Uri(_baseUri, svgSource);
                }
                else
                {
                    string asmName = assembly.GetName().Name;
                    string uriString = String.Format(
                        "pack://application:,,,/{0};component/{1}",
                        asmName, svgPath);

                    return new Uri(uriString);
                }
            }
        }

        private static void OnSourceChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            SvgCanvas viewbox = obj as SvgCanvas;
            if (viewbox == null)
            {
                return;
            }

            viewbox._sourceUri = (Uri)args.NewValue;
            if (viewbox._sourceUri == null)
            {
                viewbox.OnUnloadDiagram();
            }
            else
            {
                viewbox.OnSettingsChanged();
            }
        }

        #endregion

        #region IUriContext Members

        /// <summary>
        /// Gets or sets the base URI of the current application context.
        /// </summary>
        /// <value>
        /// The base URI of the application context.
        /// </value>
        public Uri BaseUri
        {
            get {
                return _baseUri;
            }
            set {
                _baseUri = value;
            }
        }

        #endregion
    }
}
