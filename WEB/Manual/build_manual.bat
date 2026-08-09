call pandoc USER_MANUAL.md -o USER_MANUAL.pdf --pdf-engine=xelatex -V header-includes="\usepackage{float}" -V geometry:margin=1cm -V papersize:a4 -V header-includes="\usepackage{float}"
