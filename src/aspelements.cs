//
// aspelements.cs: classes to encapsulate tags, plaintext, specialized tags...
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//
// Licensed under the terms of the GNU GPL
//
// (C) 2002 Ximian, Inc (http://www.ximian.com)
//

namespace Mono.ASP {
	using System;
	using System.Collections;
	using System.Text;
	using System.Web.UI.HtmlControls;
	using System.Web.UI.WebControls;
	
public enum ElementType {
	TAG,
	PLAINTEXT
}

public abstract class Element {
	private ElementType elementType;

	public Element (ElementType type)
	{
		elementType = type;
	}
	
	public ElementType GetElementType
	{
		get { return elementType; }
	}
} // class Element

public class PlainText : Element {
	private StringBuilder text;

	public PlainText () : base (ElementType.PLAINTEXT)
	{
		text = new StringBuilder ();
	}

	public PlainText (StringBuilder text) : base (ElementType.PLAINTEXT)
	{
		this.text = text;
	}

	public PlainText (string text) : this ()
	{
		this.text.Append (text);
	}

	public void Append (string more)
	{
		text.Append (more);
	}
	
	public string Text
	{
		get { return text.ToString (); }
	}

	public override string ToString ()
	{
		return "PlainText: " + Text;
	}
}

public enum TagType {
	DIRECTIVE,
	HTML,
	HTMLCONTROL,
	SERVERCONTROL,
	INLINEVAR,
	INLINECODE,
	CLOSING,
	NOTYET
}

public class TagAttributes : Hashtable {
	public TagAttributes () :
		base (new CaseInsensitiveHashCodeProvider (), new CaseInsensitiveComparer ())
	{
	}

	public bool IsRunAtServer ()
	{
		if (!Contains ("runat"))
			return false;

		return (0 == String.Compare ((string) this ["runat"], "server", true));
	}

	public override string ToString ()
	{
		string ret = "";
		foreach (string key in Keys){
			ret += key + "=" + (string) this [key] + " ";
		}

		return ret;
	}
}

public class Tag : Element {
	protected string tag;
	protected TagType tagType;
	protected TagAttributes attributes;
	protected bool self_closing;

	internal Tag (Tag other) :
		  base (ElementType.TAG)
	{
		this.tag = other.tag;
		this.tagType = other.tagType;
		this.attributes = other.attributes;
		this.self_closing = other.self_closing;
	}

	public Tag (string tag, TagAttributes attributes, bool self_closing) :
		  base (ElementType.TAG)
	{
		if (tag == null)
			throw new ArgumentNullException ();

		this.tag = tag;
		this.attributes = attributes;
		this.tagType = TagType.NOTYET;
		this.self_closing = self_closing;
	}
	
	public string TagID
	{
		get { return tag; }
	}

	public TagType TagType
	{
		get { return tagType; }
	}

	public bool SelfClosing
	{
		get { return self_closing; }
	}

	public TagAttributes Attributes
	{
		get { return attributes; }
	}

	public string PlainHtml
	{
		get {
			StringBuilder plain = new StringBuilder ();
			plain.Append ('<');
			if (tagType == TagType.CLOSING)
				plain.Append ('/');
			plain.Append (tag);
			if (attributes != null){
				plain.Append (' ');
				foreach (string key in attributes.Keys){
					plain.Append (key);
					if (attributes [key] != null){
						plain.Append ("=\"");
						plain.Append ((string) attributes [key]);
						plain.Append ("\" ");
					}
				}
			}
			
			if (self_closing)
				plain.Append ('/');
			plain.Append ('>');
			return plain.ToString ();
		}
	}

	public override string ToString ()
	{
		return TagID + " " + Attributes + " " + self_closing;
	}
}

public class CloseTag : Tag {
	public CloseTag (string tag) : base (tag, null, false)
	{
		tagType = TagType.CLOSING;
	}
}

public class Directive : Tag {
	private static Hashtable directivesHash;

	static Directive ()
	{
		InitHash ();
	}
	
	private static void InitHash ()
	{
		directivesHash = new Hashtable (new CaseInsensitiveHashCodeProvider (),
						new CaseInsensitiveComparer ()); 

		//TODO: look for a more appropiate container instead of Hashtable
		directivesHash.Add ("PAGE", null);
		directivesHash.Add ("CONTROL", null);
		directivesHash.Add ("IMPORT", null);
		directivesHash.Add ("IMPLEMENTS", null);
		directivesHash.Add ("REGISTER", null);
		directivesHash.Add ("ASSEMBLY", null);
		directivesHash.Add ("OUTPUTCACHE", null);
		directivesHash.Add ("REFERENCE", null);
	}
	
	public Directive (string tag, TagAttributes attributes) :
	       base (tag.ToUpper (), attributes, true)
	{
		tagType = TagType.DIRECTIVE;
	}

	public static bool IsDirectiveID (string id)
	{
		return directivesHash.Contains (id);
	}
	
	public override string ToString ()
	{
		return "Directive: " + tag;
	}
}

public class HtmlControlTag : Tag {
	private Type control_type;

	private static Hashtable controls;
	private static Hashtable inputTypes;
	private static int ctrlNumber = 1;

	private static void InitHash ()
	{
		controls = new Hashtable (new CaseInsensitiveHashCodeProvider (),
					  new CaseInsensitiveComparer ()); 

		controls.Add ("A", typeof (HtmlAnchor));
		controls.Add ("BUTTON", typeof (HtmlButton));
		controls.Add ("FORM", typeof (HtmlForm));
		controls.Add ("IMAGE", typeof (HtmlImage));
		controls.Add ("INPUT", "INPUT");
		controls.Add ("SELECT", typeof (HtmlSelect));
		controls.Add ("TABLE", typeof (HtmlTable));
		controls.Add ("TD", typeof (HtmlTableCell));
		controls.Add ("TH", typeof (HtmlTableCell));
		controls.Add ("TR", typeof (HtmlTableRow));
		controls.Add ("TEXTAREA", typeof (HtmlTextArea));

		inputTypes = new Hashtable (new CaseInsensitiveHashCodeProvider (),
					    new CaseInsensitiveComparer ());

		inputTypes.Add ("BUTTON", typeof (HtmlInputButton));
		inputTypes.Add ("SUBMIT", typeof (HtmlInputButton));
		inputTypes.Add ("RESET", typeof (HtmlInputButton));
		inputTypes.Add ("CHECKBOX", typeof (HtmlInputCheckBox));
		inputTypes.Add ("FILE", typeof (HtmlInputFile));
		inputTypes.Add ("HIDDEN", typeof (HtmlInputHidden));
		inputTypes.Add ("IMAGE", typeof (HtmlInputImage));
		inputTypes.Add ("RADIO", typeof (HtmlInputRadioButton));
		inputTypes.Add ("TEXT", typeof (HtmlInputText));
		inputTypes.Add ("PASSWORD", typeof (HtmlInputText));
	}
	
	static HtmlControlTag ()
	{
		InitHash ();
	}
	
	public HtmlControlTag (string tag, TagAttributes attributes, bool self_closing) : 
		base (tag, attributes, self_closing) 
	{
		SetData ();
		if (attributes ["ID"] == null){
			object controlID = attributes ["ID"];
			if (controlID == null)
				attributes.Add ("ID", "_control" + ctrlNumber);
		}
		ctrlNumber++;
	}

	public HtmlControlTag (Tag source_tag) :
		this (source_tag.TagID, source_tag.Attributes, source_tag.SelfClosing) 
	{
	}

	private void SetData ()
	{
		tagType = TagType.HTMLCONTROL; 
		if (!(controls [tag] is string)){
			control_type = (Type) controls [tag.ToUpper ()];
			if (control_type == null)
				control_type = typeof (HtmlGenericControl);
		} else {
			string type_value = (string) attributes ["TYPE"];
			if (type_value== null)
				throw new ArgumentException ("INPUT tag without TYPE attribute!!!");

			control_type = (Type) inputTypes [type_value];
			//TODO: what does MS with this one?
			if (control_type == null)
				throw new ArgumentException ("Unknown input type -> " + type_value);
		}
	}

	public Type ControlType
	{
		get { return control_type; }
	}

	public string ControlID
	{
		get { return (string) attributes ["ID"]; }
	}

	public override string ToString ()
	{
		string ret = "HtmlControlTag: " + tag + " Name: " + ControlID + "Type:" +
			     control_type.ToString () + "\n\tAttributes:\n";

		foreach (string key in attributes.Keys){
			ret += "\t" + key + "=" + attributes [key];
		}
		return ret;
	}
}

public class Component : Tag {
	private Type type;
	private string alias;
	private string control_type;
	private bool is_close_tag;

	public Component (Tag input_tag, Type type) :
		base (input_tag)
	{
		tagType = TagType.SERVERCONTROL;
		this.is_close_tag = input_tag is CloseTag;
		this.type = type;
		int pos = input_tag.TagID.IndexOf (':');
		alias = tag.Substring (0, pos);
		control_type = tag.Substring (pos + 1);
	}

	public Type ComponentType
	{
		get { return type; }
	}
	
	public string ControlID
	{
		get { return (string) attributes ["ID"]; }
	}

	public bool IsCloseTag
	{
		get { return is_close_tag; }
	}

	public override string ToString ()
	{
		return type.ToString () + " Alias: " + alias + " ID: " + (string) attributes ["id"];
	}
}
	
}

