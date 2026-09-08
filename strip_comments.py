import sys, re

def strip_comments(text):
    out = []
    i = 0
    n = len(text)
    while i < n:
        c = text[i]
        # verbatim string @"..."
        if c == '@' and i+1 < n and text[i+1] == '"':
            out.append(text[i:i+2]); i += 2
            while i < n:
                if text[i] == '"':
                    if i+1 < n and text[i+1] == '"':
                        out.append('""'); i += 2; continue
                    else:
                        out.append('"'); i += 1; break
                out.append(text[i]); i += 1
            continue
        # regular string "..."
        if c == '"':
            out.append(c); i += 1
            while i < n:
                if text[i] == '\\' and i+1 < n:
                    out.append(text[i:i+2]); i += 2; continue
                if text[i] == '"':
                    out.append('"'); i += 1; break
                out.append(text[i]); i += 1
            continue
        # char literal '...'
        if c == "'":
            out.append(c); i += 1
            while i < n:
                if text[i] == '\\' and i+1 < n:
                    out.append(text[i:i+2]); i += 2; continue
                if text[i] == "'":
                    out.append("'"); i += 1; break
                out.append(text[i]); i += 1
            continue
        # line comment
        if c == '/' and i+1 < n and text[i+1] == '/':
            while i < n and text[i] != '\n':
                i += 1
            continue
        # block comment
        if c == '/' and i+1 < n and text[i+1] == '*':
            i += 2
            while i < n and not (text[i] == '*' and i+1 < n and text[i+1] == '/'):
                i += 1
            i += 2
            continue
        out.append(c); i += 1
    result = ''.join(out)
    # collapse lines that are now empty due to removed trailing comment, but keep intentional blank lines minimal
    lines = result.split('\n')
    cleaned = []
    prev_blank = False
    for ln in lines:
        stripped = ln.rstrip()
        if stripped.strip() == '':
            if prev_blank:
                continue
            prev_blank = True
            cleaned.append('')
        else:
            prev_blank = False
            cleaned.append(stripped)
    return '\n'.join(cleaned) + '\n'

for path in sys.argv[1:]:
    with open(path, 'r', encoding='utf-8-sig') as f:
        content = f.read()
    new_content = strip_comments(content)
    with open(path, 'w', encoding='utf-8', newline='\n') as f:
        f.write(new_content)
    print("stripped:", path)
