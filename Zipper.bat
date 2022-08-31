"C:\Program Files\7-Zip\7z" a -y -tzip %2 %1 -mx5 
"C:\Program Files\7-Zip\7z" d %2 *.config -r

echo done