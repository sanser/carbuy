﻿using OpenRPA.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.IE
{
    public class IEElement : IElement
    {
        public IEElement(Browser browser, MSHTML.IHTMLElement Element)
        {
            Browser = browser;
            RawElement = Element;
            ClassName = Element.className;
            Id = Element.id;
            TagName = Element.tagName.ToLower();
            Name = "";
            try
            {
                if (!(RawElement.getAttribute("Name") is System.DBNull))
                {
                    Name = (string)RawElement.getAttribute("Name");
                }
            }
            catch (Exception)
            {
            }
            if (TagName == "input")
            {
                MSHTML.IHTMLInputElement inputelement = Element as MSHTML.IHTMLInputElement;
                Type = inputelement.type.ToLower();
            }
            try
            {
                MSHTML.IHTMLUniqueName id = RawElement as MSHTML.IHTMLUniqueName;
                UniqueID = id.uniqueID;
            }
            catch (Exception)
            {
            }
            IndexInParent = -1;
            if (Element.parentElement != null && !string.IsNullOrEmpty(UniqueID))
            {
                MSHTML.IHTMLElementCollection children = (MSHTML.IHTMLElementCollection)Element.parentElement.children;
                for (int i = 0; i < children.length; i++)
                {
                    if (children.item(i) is MSHTML.IHTMLUniqueName id && id.uniqueID == UniqueID) { IndexInParent = i; break; }
                }
            }
        }
        public string Name { get; set; }
        public IEElement[] Children
        {
            get
            {
                var result = new List<IEElement>();
                MSHTML.IHTMLElementCollection children = (MSHTML.IHTMLElementCollection)RawElement.children;
                foreach (MSHTML.IHTMLElement c in children)
                {
                    try
                    {
                        result.Add(new IEElement(Browser, c));
                    }
                    catch (Exception)
                    {
                    }
                }
                return result.ToArray();
            }
        }

        private System.Drawing.Rectangle? _Rectangle = null;
        public System.Drawing.Rectangle Rectangle
        {
            get
            {
                if (_Rectangle != null) return _Rectangle.Value;

                _Rectangle = System.Drawing.Rectangle.Empty;
                int elementx;
                int elementy;
                int elementw;
                int elementh;
                if (!(RawElement is MSHTML.IHTMLElement2 ele)) return _Rectangle.Value;
                var col = ele.getClientRects();
                if (col == null) return _Rectangle.Value;
                try
                {
                    var _rect = col.item(0);
                    var left = ((dynamic)_rect).left;
                    var top = ((dynamic)_rect).top;
                    var right = ((dynamic)_rect).right;
                    var bottom = ((dynamic)_rect).bottom;
                    elementx = left;
                    elementy = top;
                    elementw = right - left;
                    elementh = bottom - top;


                    elementx += Browser.frameoffsetx;
                    elementy += Browser.frameoffsety;

                    elementx += Convert.ToInt32(Browser.panel.BoundingRectangle.X);
                    elementy += Convert.ToInt32(Browser.panel.BoundingRectangle.Y);
                    //var t = Task.Factory.StartNew(() =>
                    //{
                    //});
                    //t.Wait();
                    _Rectangle = new System.Drawing.Rectangle(elementx, elementy, elementw, elementh);
                    return _Rectangle.Value;
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
                return _Rectangle.Value;
            }
            set { }
        }
        public Browser Browser { get; set; }
        public string ClassName { get; set; }
        public string UniqueID { get; set; }
        public string Id { get; set; }
        public string TagName { get; set; }
        public string Type { get; set; }
        public int IndexInParent { get; set; }
        public MSHTML.IHTMLElement RawElement { get; private set; }
        object IElement.RawElement { get => RawElement; set => RawElement = value as MSHTML.IHTMLElement; }
        public void Click(bool VirtualClick, Input.MouseButton Button, int OffsetX, int OffsetY, bool DoubleClick, bool AnimateMouse)
        {
            if (Button != Input.MouseButton.Left) { VirtualClick = false; }
            if (VirtualClick)
            {
                RawElement.click();
                if (DoubleClick) RawElement.click();
            } else
            {
                NativeMethods.SetCursorPos(Rectangle.X + OffsetX, Rectangle.Y + OffsetY);
                Input.InputDriver.Click(Button);
            }
        }
        public void Focus()
        {
        }
        public Task Highlight(bool Blocking, System.Drawing.Color Color, TimeSpan Duration)
        {
            if (!Blocking)
            {
                Task.Run(() => _Highlight(Color, Duration));
                return Task.CompletedTask;
            }
            return _Highlight(Color, Duration);
        }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "IDE1006")]
        public Task _Highlight(System.Drawing.Color Color, TimeSpan Duration)
        {
            using (Interfaces.Overlay.OverlayWindow _overlayWindow = new Interfaces.Overlay.OverlayWindow(true))
            {
                _overlayWindow.BackColor = Color;
                _overlayWindow.Visible = true;
                _overlayWindow.SetTimeout(Duration);
                _overlayWindow.Bounds = Rectangle;
                var sw = new System.Diagnostics.Stopwatch();
                sw.Start();
                do
                {
                    System.Threading.Thread.Sleep(10);
                    _overlayWindow.TopMost = true;
                } while (_overlayWindow.Visible && sw.Elapsed < Duration);
                return Task.CompletedTask;
            }
        }
        public string Value
        {
            get
            {
                if (RawElement.tagName.ToLower() == "input")
                {
                    var ele = (MSHTML.IHTMLInputElement)RawElement;
                    return ele.value;
                }
                else
                {
                    return RawElement.innerText;
                }
                // return null;
            }
            set
            {
                if (RawElement.tagName.ToLower() == "input")
                {
                    var ele = (MSHTML.IHTMLInputElement)RawElement;
                    ele.value = value;
                }
                if(RawElement.tagName.ToLower() == "select")
                {
                    var ele = (MSHTML.IHTMLSelectElement)RawElement;
                    foreach(MSHTML.IHTMLOptionElement e in (dynamic)((dynamic)ele.options))
                    {
                        if(e.value == value)
                        {
                            ele.value = value;
                        } else if (e.text == value)
                        {
                            ele.value = e.value;
                        }
                    }
                }
            }
        }
        public override string ToString()
        {
            return TagName + " " + (!string.IsNullOrEmpty(Id) ? Id : ClassName);
        }
        public override bool Equals(object obj)
        {
            if (!(obj is IEElement e)) return false;
            if (e.UniqueID == UniqueID) return true;
            if (RawElement.sourceIndex == e.RawElement.sourceIndex) return true;
            if (RawElement.GetHashCode() == e.RawElement.GetHashCode()) return true;
            return false;
            //return base.Equals(obj);
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        public string ImageString()
        {
            var AddedWidth = 10;
            var AddedHeight = 10;
            var ScreenImageWidth = Rectangle.Width + AddedWidth;
            var ScreenImageHeight = Rectangle.Height + AddedHeight;
            var ScreenImagex = Rectangle.X - (AddedWidth / 2);
            var ScreenImagey = Rectangle.Y - (AddedHeight / 2);
            if (ScreenImagex < 0) ScreenImagex = 0; if (ScreenImagey < 0) ScreenImagey = 0;
            using (var image = Interfaces.Image.Util.Screenshot(ScreenImagex, ScreenImagey, ScreenImageWidth, ScreenImageHeight, Interfaces.Image.Util.ActivityPreviewImageWidth, Interfaces.Image.Util.ActivityPreviewImageHeight))
            {
                return Interfaces.Image.Util.Bitmap2Base64(image);
            }
        }
        public string Href
        {
            get
            {
                if (RawElement.getAttribute("href") is System.DBNull) return null;
                return (string)RawElement.getAttribute("href");
            }
        }
        public string Src
        {
            get
            {
                if (RawElement.getAttribute("src") is System.DBNull) return null;
                return (string)RawElement.getAttribute("src");
            }
        }
        public string Alt
        {
            get
            {
                if (RawElement.getAttribute("alt") is System.DBNull) return null;
                return (string)RawElement.getAttribute("alt");
            }
        }
        public IElement[] Items
        {
            get
            {
                var result = new List<IElement>();
                if (RawElement.tagName.ToLower() == "select")
                {

                }
                return result.ToArray();
            }
        }

    }
}
