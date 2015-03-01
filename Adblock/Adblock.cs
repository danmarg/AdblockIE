using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using MSHTML;
using SHDocVw;
using System.Text.RegularExpressions;
using System.Diagnostics;
using Microsoft.Win32;
using System.IO;

namespace af0.Adblock
{
    /// <summary>
    /// Contains actual DOM traversal, rule parsing, and filtering logic. 
    /// </summary>
    sealed class AdblockEngine
    {
        // implement singleton

        private static volatile AdblockEngine instance;
        private static object syncRoot = new Object();

        public static AdblockEngine Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                            instance = new AdblockEngine();
                    }
                }
                return instance;
            }
        }

        enum Action{Block, Pass};
        enum ApplyTo { Image, Div, Script, Link, Frame, Object, All };
        class Rule
        {
            public string site = null;
            public Glob pattern = new Glob("*");
            public Action action = Action.Block;
            public ApplyTo applyto = ApplyTo.All;

            public bool IsMatch(Uri url, string property, ApplyTo tokentype)
            {
                if (url == null)
                    return false;
                return (applyto == ApplyTo.All || applyto == tokentype) &&
                       (String.IsNullOrEmpty(site) || url.Host.EndsWith(site)) &&
                       (pattern.IsMatch(property));                       
            }

            public Rule(XElement e)
            {
                bool ruleWasNotEmpty = false;
                XAttribute site = e.Attribute(XName.Get("site"));
                XAttribute applyto = e.Attribute(XName.Get("applyto"));
                XAttribute action = e.Attribute(XName.Get("action"));
                string pattern = e.Value;
                if (site != null)
                {
                    this.site = site.Value;
                    ruleWasNotEmpty = true;
                }
                if (applyto != null)
                {
                    switch (applyto.Value)
                    {
                        case "image":
                            this.applyto = ApplyTo.Image;
                            break;
                        case "div":
                            this.applyto = ApplyTo.Div;
                            break;
                        case "script":
                            this.applyto = ApplyTo.Script;
                            break;
                        case "link":
                            this.applyto = ApplyTo.Link;
                            break;
                        case "frame":
                            this.applyto = ApplyTo.Frame;
                            break;
                        case "object":
                            this.applyto = ApplyTo.Object;
                            break;
                        case "all":
                            this.applyto = ApplyTo.All;
                            break;
                        default:
                            throw new ArgumentException("Invalid \"applyto\" attribute value " + applyto.Value);
                    }
                    ruleWasNotEmpty = true;
                }
                if (action != null)
                {
                    switch (action.Value)
                    {
                        case "block":
                            this.action = Action.Block;
                            break;
                        case "pass":
                            this.action = Action.Pass;
                            break;
                        default:
                            throw new ArgumentException("Invalid \"action\" value " + action.Value);
                    }
                    ruleWasNotEmpty = true;
                }
                if (!String.IsNullOrEmpty(pattern))
                {
                    ruleWasNotEmpty = true;
                    this.pattern = new Glob(pattern);
                }
                if (!ruleWasNotEmpty)
                    throw new ArgumentException("Invalid empty rule given");
            }
        }

        // the actual patterns
        Rule[] _imageBlacklist = new Rule[0];
        Rule[] _imageWhitelist = new Rule[0];
        Rule[] _divBlacklist = new Rule[0];
        Rule[] _divWhitelist = new Rule[0];
        Rule[] _scriptBlacklist = new Rule[0];
        Rule[] _scriptWhitelist = new Rule[0];
        Rule[] _linkBlacklist = new Rule[0];
        Rule[] _linkWhitelist = new Rule[0];
        Rule[] _frameBlacklist = new Rule[0];
        Rule[] _frameWhitelist = new Rule[0];
        Rule[] _objectBlacklist = new Rule[0];
        Rule[] _objectWhitelist = new Rule[0];
        // black and whit`elists for all elements
        Rule[] _allBlacklist = new Rule[0];
        Rule[] _allWhitelist = new Rule[0];

        uint _blocked = 0;

        const string REG_KEY = "Software\\af0\\Adblock";

        private static string GetBlacklistPath()
        {
            string blacklistPath = Path.Combine(Path.GetDirectoryName(typeof(AdblockEngine).Assembly.Location), "blacklist.xml"); // default value

            try
            {
                RegistryKey configKey = Registry.CurrentUser.CreateSubKey(REG_KEY, RegistryKeyPermissionCheck.ReadSubTree);
                blacklistPath = GetConfigString(configKey, "Blacklist", blacklistPath);
            }
            catch (Exception e)
            {
                Trace.Fail(e.ToString());
            }
            return blacklistPath;
        }
        /// <summary>
        /// Never loads blacklist. FOR TESTING PURPOSES ONLY. 
        /// </summary>
        /// <param name="donotloadblacklist"></param>
        private AdblockEngine(object DONOTLOADBLACKLIST)
        {
        }
        private AdblockEngine() : this(GetBlacklistPath())
        {
        }
        private AdblockEngine(String blacklistPath)
        {
            if (!File.Exists(blacklistPath))
            {
                Trace.Fail(String.Format("Blacklist file {0} does not exist", blacklistPath));
            }
            else
            {
                XDocument doc = XDocument.Load(blacklistPath);

                List<Rule> rules = new List<Rule>();
                foreach (XElement e in from c in doc.Root.Elements() where c.Parent.Name == "Adblock" && c.Name == "rule" select c)
                {
                    Rule r;
                    try
                    {
                        r = new Rule(e);
                    }
                    catch (Exception ex)
                    {
                        Trace.Fail(String.Format("Bad rule {0}: {1}", e.ToString(), ex.Message));
                        continue;
                    }
                    rules.Add(r);
                }

                // now put all the rules in the right array to speed up filtering later
                _allBlacklist = (from rule in rules where rule.action == Action.Block && rule.applyto == ApplyTo.All select rule).ToArray();
                _allWhitelist = (from rule in rules where rule.action == Action.Pass && rule.applyto == ApplyTo.All select rule).ToArray();
                _imageBlacklist = (from rule in rules where rule.action == Action.Block && rule.applyto == ApplyTo.Image select rule).ToArray();
                _imageWhitelist = (from rule in rules where rule.action == Action.Pass && rule.applyto == ApplyTo.Image select rule).ToArray();
                _divBlacklist = (from rule in rules where rule.action == Action.Block && rule.applyto == ApplyTo.Div select rule).ToArray();
                _divWhitelist = (from rule in rules where rule.action == Action.Pass && rule.applyto == ApplyTo.Div select rule).ToArray();
                _scriptBlacklist = (from rule in rules where rule.action == Action.Block && rule.applyto == ApplyTo.Script select rule).ToArray();
                _scriptWhitelist = (from rule in rules where rule.action == Action.Pass && rule.applyto == ApplyTo.Script select rule).ToArray();
                _linkBlacklist = (from rule in rules where rule.action == Action.Block && rule.applyto == ApplyTo.Link select rule).ToArray();
                _linkWhitelist = (from rule in rules where rule.action == Action.Pass && rule.applyto == ApplyTo.Link select rule).ToArray();
                _frameBlacklist = (from rule in rules where rule.action == Action.Block && rule.applyto == ApplyTo.Frame select rule).ToArray();
                _frameWhitelist = (from rule in rules where rule.action == Action.Pass && rule.applyto == ApplyTo.Frame select rule).ToArray();
                _objectBlacklist = (from rule in rules where rule.action == Action.Block && rule.applyto == ApplyTo.Object select rule).ToArray();
                _objectWhitelist = (from rule in rules where rule.action == Action.Pass && rule.applyto == ApplyTo.Object select rule).ToArray();
            }
        }

        // Getting a configuration file:
		private static string GetConfigString(RegistryKey key, string name, string defaultValue)
		{
            string value = key.GetValue(name, String.Empty).ToString();
            if(String.IsNullOrEmpty(value))
                return defaultValue;
            else
                return value;
        }

        /// <summary>
        /// Whether or not a rule exists to block a given node
        /// </summary>
        /// <param name="url">Host site URL</param>
        /// <param name="property">This is usually an image src, link href, whatever, but it could be a DIV class or something</param>
        /// <param name="node">Type of node this is</param>
        /// <returns></returns>
        private bool ShouldBlock(string locationUrl, string property, ApplyTo node)
        {
            Uri locationUri = ParseUri(locationUrl);
            if (locationUri == null || property == null)
            {
                Trace.WriteLine(String.Format("ShouldBlock({0}) = false", locationUri));
                return false;
            }

            Rule[] blacklist = null;
            Rule[] whitelist = null;
            switch (node)
            {
                case ApplyTo.Div:
                    blacklist = _divBlacklist;
                    whitelist = _divWhitelist;
                    break;
                case ApplyTo.Frame:
                    blacklist = _frameBlacklist;
                    whitelist = _frameWhitelist;
                    break;
                case ApplyTo.Image:
                    blacklist = _imageBlacklist;
                    whitelist = _imageWhitelist;
                    break;
                case ApplyTo.Link:
                    blacklist = _linkBlacklist;
                    whitelist = _linkWhitelist;
                    break;
                case ApplyTo.Object:
                    blacklist = _objectBlacklist;
                    whitelist = _objectWhitelist;
                    break;
                case ApplyTo.Script:
                    blacklist = _objectBlacklist;
                    whitelist = _objectWhitelist;
                    break;
                default:
                    Debug.Fail("Unknown node type in ShouldBlock");
                    break;
            }

            if (Array.Exists(whitelist, (x => x.IsMatch(locationUri, property, node))) ||
                Array.Exists(_allWhitelist, (x => x.IsMatch(locationUri, property, node))))
            {
                Trace.WriteLine(String.Format("ShouldBlock({0}, {1}) = false (whitelist)", locationUri, property));
                return false;
            }
            else if (Array.Exists(blacklist, (x => x.IsMatch(locationUri, property, node))) ||
                     Array.Exists(_allBlacklist, (x => x.IsMatch(locationUri, property, node))))
            {
                Trace.WriteLine(String.Format("ShouldBlock({0}, {1}) = true (blacklist)", locationUri, property));
                return true;
            }
            else
            {
                Trace.WriteLine(String.Format("ShouldBlock({0}, {1}) = false (no match)", locationUri, property));
                return false;
            }
        }
        private void Remove(IHTMLDOMNode n)
        {
            _blocked++;
            try
            {
                n.parentNode.removeChild(n);
            }
            catch { }
        }

        private static Uri ParseUri(string uri)
        {
            Uri r;
            if (Uri.TryCreate(uri, UriKind.Absolute, out r))
                return r;
            return null;
        }
        private static string NormalizeUrl(string baseurl, string path)
        {
            Uri p; Uri b;
            if (Uri.TryCreate(path, UriKind.RelativeOrAbsolute, out p))
            {
                if (!p.IsAbsoluteUri && Uri.TryCreate(baseurl, UriKind.Absolute, out b))
                {
                    Uri.TryCreate(b, path, out p);
                }
                return p.AbsoluteUri;
            }
            return path;
        }

        private void FilterPage(HTMLDocument doc, string url)
        {
            Trace.WriteLine(String.Format("Filter page {0}", doc.url));
            // For the below, we do whatever we have to to pick out the "property" of this element type (usually src or href or something). 
            // If the property is null or empty, we use the currently URL (note: this may be a stupid thing to do for some things, like frames, but it makes sense for 
            // others, like <script>, which if missing a src means it's embedded on the current page). 
            string s;
            foreach (IHTMLImgElement e in doc.images)
            {
                try
                {
                    s = e.src;
                    if (string.IsNullOrEmpty(s))
                        s = url;
                    if (ShouldBlock(url, NormalizeUrl(url, s), ApplyTo.Image))
                        Remove(e as IHTMLDOMNode);
                }
                catch { }
            }
            foreach (IHTMLEmbedElement e in doc.embeds)
            {
                try
                {
                    s = e.src;
                    if (string.IsNullOrEmpty(s))
                        s = url;
                    if (ShouldBlock(url, NormalizeUrl(url, s), ApplyTo.Object)) // do I need a separate classification for embeds? I don't think anyone cares for that level of specificity in the rules
                        Remove(e as IHTMLDOMNode);
                }
                catch { }
            }
            foreach (IHTMLScriptElement e in doc.scripts)
            {
                try
                {
                    s = e.src;
                    if (string.IsNullOrEmpty(s))
                        s = url;
                    if (ShouldBlock(url, NormalizeUrl(url, s), ApplyTo.Script))
                        Remove(e as IHTMLDOMNode);
                }
                catch { }
            }
            foreach (IHTMLElement e in doc.links)
            {
                try
                {
                    if (e is IHTMLLinkElement)
                    {
                        s = (e as IHTMLLinkElement).href;
                        if (string.IsNullOrEmpty(s))
                            s = url;
                        if (ShouldBlock(url, NormalizeUrl(url, s), ApplyTo.Link))
                            Remove(e as IHTMLDOMNode);
                    }
                    else if (e is IHTMLAnchorElement)
                    {
                        s = (e as IHTMLAnchorElement).href;
                        if(string.IsNullOrEmpty(s))
                            s = url;
                        if (ShouldBlock(url, NormalizeUrl(url, s), ApplyTo.Link))
                            Remove(e as IHTMLDOMNode);
                    }
                }
                catch { }
            }
            foreach (IHTMLElement2 e in doc.getElementsByTagName("OBJECT")) // slightly more complicated for flash, etc
            {
                foreach (IHTMLElement4 c in e.getElementsByTagName("PARAM"))
                {
                    try
                    {
                        if ("Src".Equals((string)c.getAttributeNode("NAME").nodeValue, StringComparison.CurrentCultureIgnoreCase) || "Movie".Equals((string)c.getAttributeNode("NAME").nodeValue, StringComparison.CurrentCultureIgnoreCase))
                        {
                            s = c.getAttributeNode("VALUE").nodeValue as string;
                            if (string.IsNullOrEmpty(s))
                                s = url;
                            if (ShouldBlock(url, NormalizeUrl(url, s), ApplyTo.Object))
                            {
                                Remove(e as IHTMLDOMNode);
                            }
                        }
                    }
                    catch { }
                }
            }
            foreach (IHTMLElement4 e in doc.getElementsByTagName("IFRAME"))
            {
                try
                {
                    s = e.getAttributeNode("src").nodeValue as string;
                    if (ShouldBlock(url, NormalizeUrl(url, s), ApplyTo.Frame))
                    {
                        Remove(e as IHTMLDOMNode);
                    }
                    else
                    {
                        // recursively filter--it's a bit silly to do this here, but the idea is to 
                        // allow us to run in the DownloadComplete event instead of in DocumentComplete, to deal with F5, 
                        // and thus lets us deal with DownloadComplete not getting the document argument
                        IWebBrowser2 f = e as IWebBrowser2;
                        if (f != null)
                            FilterPage(f.Document as HTMLDocument, (f.Document as HTMLDocument).url);
                    }
                }
                catch { }                
            }
            foreach (IHTMLElement4 e in doc.getElementsByTagName("FRAME"))
            {
                try
                {
                    s = e.getAttributeNode("src").nodeValue as string;
                    if (ShouldBlock(url, NormalizeUrl(url, s), ApplyTo.Frame))
                    {
                        Remove(e as IHTMLDOMNode);
                    }
                    else
                    {
                        // recursively filter--it's a bit silly to do this here, but the idea is to 
                        // allow us to run in the DownloadComplete event instead of in DocumentComplete, to deal with F5, 
                        // and thus lets us deal with DownloadComplete not getting the document argument
                        IWebBrowser2 f = e as IWebBrowser2;
                        if (f != null)
                            FilterPage(f.Document as HTMLDocument, (f.Document as HTMLDocument).url);
                    }
                }
                catch { }
            }
            foreach (IHTMLElement4 e in doc.getElementsByTagName("DIV"))
            {
                try
                {
                    s = e.getAttributeNode("class").nodeValue as string;
                    if (ShouldBlock(url, s, ApplyTo.Div))
                    {
                        Remove(e as IHTMLDOMNode);
                    }
                }
                catch { }
            }
        }

        private delegate void FilterPageDelegate(HTMLDocument doc, string url);

        /// <summary>
        /// asynchronously filter after the page has loaded
        /// </summary>
        /// <param name="document"></param>
        /// <param name="url"></param>
        public void Filter(HTMLDocument document, string url)
        {
            try
            {
                // run once now
                //FilterPageDelegate d = new FilterPageDelegate(FilterPage);
                //d.BeginInvoke(document, url, null, d);
                FilterPage(document, url);
            }
            catch (Exception e)
            {
                Trace.Fail(e.ToString());
            }
        }

        /// <summary>
        /// synchronously filter elements before onload runs
        /// </summary>
        /// <param name="document"></param>
        /// <param name="url"></param>
        public void PreFilter(HTMLDocument document, string url)
        {
            try
            {
                string s;
                foreach (IHTMLScriptElement e in document.scripts)
                {
                    s = e.src;
                    if (string.IsNullOrEmpty(s))
                        s = url;
                    if (ShouldBlock(url, s, ApplyTo.Script))
                        Remove(e as IHTMLDOMNode);
                }
            }
            catch (Exception e)
            {
                Trace.WriteLine("PreFilter exception: " + e.ToString());
            }
        }
    }
}
