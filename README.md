# CMD_CONVERT_TXT_PDF

### EJECUTABLE: CMD_CONVERT_TXT_PDF.exe

##### FUNCIONAMIENTO
Este programa convierte automáticamente todos los archivos **“.TXT”, “.BMP”, “.JPG” y “.JPEG”** que estén en la carpeta **“TXT_TO_PDF”** a formato **PDF**.

##### ESTRUCTURA DE CARPETAS
El programa debe estar en una carpeta con esta estructura:
```
CMD_CONVERT_TXT_PDF.exe
TXT_TO_PDF/          <-- Aquí pones los archivos .txt, .bmp, .jpg, .jpeg a convertir
Files-txt/           <-- Aquí se genera el log automáticamente
    LOG_CMD_TXT_PDF  <-- Archivo de registro
```

##### CÓMO USARLO
1. Coloca los archivos **“.txt”, “.bmp”, “.jpg” o “.jpeg”** que quieras convertir dentro de la carpeta **“TXT_TO_PDF”**
2. Ejecuta **CMD_CONVERT_TXT_PDF.exe**
3. El programa creará un **“.pdf”** por cada archivo en la misma carpeta **“TXT_TO_PDF”**
4. Se registrará todo en **“Files-txt\LOG_CMD_TXT_PDF”**
