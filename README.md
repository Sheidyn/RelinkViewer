# RelinkViewer
 Repository to facilitate extracting and viewing Relink files

 This is made with .NET 8.0 and WPF, so make sure you have those. If you need any more dependencies let me know and i will list them down.

 **How It Works**

You will need:

- GBFR Installed
- At least the 1.0.1 version of GBFRDataTools by Nenkai: https://github.com/Nenkai/GBFRDataTools/releases

This is just an interface, the extraction work it is done by the tools themselves, this is just a way to make it easier to said such console.
IT will first ask you for the game directoy, and after that you will be asked to locate "GBFRDataTools.exe", this is essential for extracting files.

If everything goes well, you should see the directory that is provided via filelist.txt, You can search and navigate the files, however it is currently unoptimized and a bit laggy, so i apologize if it's not super smooth, if people like this tool i will try to 
make it run better, i did not spend a lot of time on this lol

You will have 3 options when rightclicking a file:

- Copy Name: Just the name of the file
- Copy Path: Copies the full path
- Extract (if it's not a folder): Will proceed to extract said file in the output directory.

The output directory can be edited in the settings.ini that gets created upon first execution.

The Searchbar allows you to look for any file that has that string in it. If you double click the item on the list, it *should* close the dialog and directly take you to wherever that file or folder is stored.

If people like this or fiind it useful i will improve on it! This is more of a personal thing i use right now, but maybe it's of use for someone else.
 
