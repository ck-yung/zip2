# Syntax 

## v2.1.1.0

## List Command

### Help

```zip2 -t?```

### Syntax:
```
List zip file:
  zip2 -t ZIP-FILE [OPTION ..] [WILD ..]
  zip2 -t [OPTION] --file ZIP-FILE [OPTION ..] [WILD ..]
OPTION:
         --file  -f  ZIP-FILENAME
      --verbose  -v  
    --name-only      
    --total-off      
         --excl      WILD[,WILD,..]
   --if-find-on  -F  DIR
  --size-format      short | WIDTH
     --show-crc      
         --sort  -o  name | date | size | ratio | last | count
          --sum      ext | dir
     --dir-only      
   --files-from  -T  FILES-FROM
       --format      auto | zip | rar
    --multi-vol  -m  	 rar only
SHORTCUT:
  -b   --verbose --name-only --total-off
  -R   --format rar
  -Z   --format zip
  ```

## Extract Command

### Help

```zip2 -x?```

### Syntax:
```
Extract zip file:
  zip2 -x  ZIP-FILE [OPTION ..] [WILD ..]
  zip2 -x [OPTION ..] --file ZIP-FILE [OPTION ..] [WILD ..]

Output dir will be 'ZIP-FILE' if '--out-dir -'
OPTION:
         --file  -f  ZIP-FILENAME
      --verbose  -v  
        --quiet  -q  
    --total-off      
       --no-dir      
      --no-time      
    --overwrite  -o  
      --out-dir  -O  OUTPUT-DIR (- to ZIP-FILENAME, = to console)
         --excl      WILD[,WILD,..]
   --files-from  -T  FILES-FROM
       --format      auto | zip | rar
    --multi-vol  -m  	 rar only
SHORTCUT:
  -b   --verbose --name-only --total-off
  -R   --format rar
  -Z   --format zip
```


## Create Zip Command

### Help
```zip2 -c?```

### Syntax:
```
Create zip file:
  zip2 -c NEW-ZIP [OPTION ..] FILE [FILE ..]
  zip2 -c --file NEW-ZIP [OPTION ..] FILE [FILE ..]
  zip2 -c NEW-ZIP [OPTION ..] -T - [FILE ..]

For example,
  dir2 src -bsk --within 2hour | zip2 -c3f ../today.zip -T -

OPTION:
         --file  -f  ZIP-FILENAME
      --verbose  -v  
    --total-off      
        --level      storage | faster | fast | smallest
   --files-from  -T  FILES-FROM
SHORTCUT:
  -0   --level storage
  -1   --level faster
  -2   --level fast
  -3   --level smallest
```