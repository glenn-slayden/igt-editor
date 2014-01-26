
There are two executables related to IGT editing, the converter
(igt-convert) and the editor (igt-edit). Both require a version
of Windows compatible with .NET 4.5 (that is, Vista or later, Windows XP 
is not supported). Windows 8 and later include the required version. For 
Windows 7 and Vista you may need to install .NET 4.5 from:

http://www.microsoft.com/en-us/download/details.aspx?id=30653

For more information on .NET 4.5:

http://msdn.microsoft.com/en-us/library/5a4x27ek(v=vs.110).aspx

igt-convert
===========
This program batch-converts an entire directory of igt-text (e.g. ODIN) 
files into the XAML format used by the editor. This conversion only
needs to be done once; the editor reads and writes the XAML format.

igt-convert usage message
=========================
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

igt-edit
========
The editor is a graphical application which allows free-form, random
access editing of multiple IGT corpora, each containing multiple IGT 
instances.

The editor reads and writes XAML-igt files. This hierarchical XML file 
format is human readable and can be edited by hand or with scripts,
although care must be taken to adhere to maintain readability of the
object graph.

Sources
=======
You can build both projects from source with Visual Studio 2013 Professional, 
which is available as a free download to current UW students at 
https://www.dreamspark.com/Student/Default.aspx

github source repository for igt-edit and igt-convert: 

https://github.com/glenn-slayden/igt-editor/

The editor is a .NET/WPF (Windows Presentation Foundation) graphical 
application which requires Windows and .NET 4.5.

Although the converter is a console application, it also cannot be built 
under Mono since it depends on the WPF objects used by the editor app.
