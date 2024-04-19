# HISTORY

[Link to Syntax](https://github.com/ck-yung/zip2/blob/main/Syntax.md)

## v2.1.2.0 (Coming)
1. Read console input (stdin) to "List" and "Extract" command if "--file" is -
2. Write console output (stdout) to "Create" command if "--file" is -
3. ??? Handle "WILD" other than "FILE" to "Create" command.
4. Support ```.tar``` and ```.tar.gz```

## v2.1.1.0
1. Add feature for command ```List``` as the default command.
2. Option output dir ```--out-dir```,```-O``` takes ```=``` for console output (stdout).
3. New option ```--multi-vol```,```-m``` is added for RAR archive files.
4. Take first command line arg as ```ZIP-FILENAME``` if ```--file```,```-f``` is NOT given

April 2023

## v2.0.2.2
1. Accept wild filename to ```--file```. (Windows OS)
e.g. ```zip2 -tvf obj\backup*.zip```
2. Add option ```--format``` to list and Extract command.
3. Option ```--show-crc``` is added to List command.
4. Shadow filenames are used to Create and Extract commands.
5. Add option ```--no-time``` to Exract command.

## v2.0.2.1
1. Support to list the content of a RAR file.
2. Support to extract content from a RAR file.
3. New options to list file:
```
   -b           List filename only
   -F DIR       List filename if it is found on DIR
   -o name      Sort by filenmae
   --dir-only   Show dir only
```
4. New option to extract file:
```
   --no-dir     Remove the original dir
```

## v2.0.1.0
Update to .NET 8

Jan 2023