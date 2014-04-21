IGT Editor
==========

Editor for IGT instances and corpora (WPF)


There are two executables related to IGT editing, the converter
(igt-convert) and the editor (igt-edit). Both require a version
of Windows compatible with .NET 4.5 (that is, Vista or later, Windows XP 
is not supported). Windows 8 and later include the required version. For 
Windows 7 and Vista you may need to install .NET 4.5.1 from:

http://www.microsoft.com/en-us/download/details.aspx?id=30653

For more information on .NET 4.5.1:

http://msdn.microsoft.com/en-us/library/5a4x27ek(v=vs.110).aspx

igt-convert (igt-convert.exe)
=============================
This console mode program batch-converts an entire directory of 
igt-text (e.g. ODIN) files into the XAML format used by the editor.
This conversion only needs to be done once; the editor reads and 
writes the XAML IGT format.

The conversion includes a few pre-configured conversion operations:
- A single TextTier is created to preserve the original source text.
- Tiers are grouped according to tier type, such that one TierGroupTier
is created for each distinct type. Source lines tagged with multiple
tier types are repeated (copied) and placed into each indicated group.

ODIN test and train data from the aggregation project has been pre-
converted and can be found in the odin-xaml directory.

XAML-IGT format
===============
The following is a sample of the XAML-igt format which the igt-edit
program reads and writes. This example shows the results of the 
built-in conversion operations mentioned above.

&lt;IgtCorpus xmlns="clr-namespace:xie;assembly=igt-xaml"
           Name="_44e4ed39704741aaba1aee6da3099b28"
           Delimiter=" "&gt;
  &lt;IgtCorpus.Items&gt;
    &lt;Igt Name="_7f5d8be3165c475f86dd39638237fa47"
         DocId="342"
         Language="french (fra)"
         FromLine="1193"
         ToLine="1195"&gt;
      &lt;Igt.Tiers&gt;
        &lt;TextTier Name="_2b0453acfed14dbfb3e9bb2f35f308c4"
                  Text="doc_id=342 1193 1195 L G T&#xD;&#xA;language: french (fra)&#xD;&#xA;line=1193 tag=L: (i) Le livre  a eu été publié.&#xD;&#xA;line=1194 tag=G: the book has had been  published&#xD;&#xA;line=1195 tag=T: `The book has had been published'"
                  TierType="odin-txt" /&gt;
        &lt;TierGroupTier Name="_dda8249bd2b244cc860ae08db11ec885"
                       TierType="Lang"&gt;
          &lt;TierGroupTier.Tiers&gt;
            &lt;TextTier Name="_6ab947a639214e7e9b0a0c3597467a23"
                      Text=" (i) Le livre  a eu été publié."
                      TierType="L-1193" /&gt;
          &lt;/TierGroupTier.Tiers&gt;
        &lt;/TierGroupTier&gt;
        &lt;TierGroupTier Name="_9471e64c3e3f44b0b96accf440dd18c5"
                       TierType="Gloss"&gt;
          &lt;TierGroupTier.Tiers&gt;
            &lt;TextTier Name="_a83b81a3b313413da4942cabaceb11f8"
                      Text=" the book has had been  published"
                      TierType="G-1194" /&gt;
          &lt;/TierGroupTier.Tiers&gt;
        &lt;/TierGroupTier&gt;
        &lt;TierGroupTier Name="_d1e89f9001ff4ce8a6493c0b2d1f725d"
                       TierType="Transl."&gt;
          &lt;TierGroupTier.Tiers&gt;
            &lt;TextTier Name="_6b82effdadd04c899c1f9d390b58474c"
                      Text=" `The book has had been published'"
                      TierType="T-1195" /&gt;
          &lt;/TierGroupTier.Tiers&gt;
        &lt;/TierGroupTier&gt;
      &lt;/Igt.Tiers&gt;
    &lt;/Igt&gt;
    &lt;!-- ... any number of additional IGT instances ... --&gt;
  &lt;/IgtCorpus.Items&gt;
&lt;/IgtCorpus&gt;
  
The 'xmlns' (namespace) directive on the root element of the document is 
required in every XAML-igt file as shown.

Support for the XAML-IGT format is provided by the igt-xaml.dll class
library.

igt-convert usage
=================
igt-convert.exe [input-dir] [output-dir]

Converts each ODIN text format IGT file (*.txt) in the input directory to a
XAML IGT file (*.xml) in the output directory. Existing files with a
conflicting name in the output directory are overwritten.

ODIN text files are UTF-8 files containing zero or more IGT instances which
adhere to the (e.g.) following format. Instances must be separated by a
blank line. Line feed format can be either Unix or DOS/Windows.

doc_id=807 2764 2766 L G T
language: spanish (spa) + english (eng)
line=2764 tag=L:         (77a) *Juan está eat-iendo
line=2765 tag=G:               Juan be/1Ss eat-DUR
line=2766 tag=T:               `Juan is eating.'

The output file format is a XAML serialization of the object graph for the
in-memory object model of the WPF IGT editor.

igt-edit (igt-edit.exe)
=======================
The editor is a graphical application which allows free-form, random
access editing of multiple IGT corpora, each containing multiple IGT 
instances.

The editor reads and writes XAML-igt files. This hierarchical XML file 
format is human readable and can be edited by hand or with scripts,
although care must be taken to adhere to maintain readability of the
object graph.

Sources
=======
github source repository for igt-edit and igt-convert: 

https://github.com/glenn-slayden/igt-editor/

The editor is a .NET/WPF (Windows Presentation Foundation) graphical 
application which requires Windows and .NET 4.5.1

Although the converter is a console application, it also cannot be built 
under Mono since it depends on the WPF objects used by the editor app.
