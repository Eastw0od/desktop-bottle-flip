import os, re

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.dirname(HERE)

src = open(os.path.join(HERE, 'game_src.html'), encoding='utf-8').read()
lf = open(os.path.join(HERE, 'lib', 'liquidfun.js'), encoding='utf-8').read()
ba = open(os.path.join(HERE, 'lib', 'bottle_assets.js'), encoding='utf-8').read()

lf_safe = lf.replace('</script>', '<\\/script>')
ba_safe = ba.replace('</script>', '<\\/script>')

out_html = src.replace('<!-- BOTTLE_ASSETS_PLACEHOLDER -->', f'<script>\n{ba_safe}\n</script>')
out_html = out_html.replace('<!-- LIQUIDFUN_INLINE_PLACEHOLDER -->', f'<script>\n{lf_safe}\n</script>')

out_path = os.path.join(REPO, 'index.html')
open(out_path, 'w', encoding='utf-8').write(out_html)

kb = os.path.getsize(out_path) / 1024
ext = re.findall(r'<(?:script|link|img)[^>]*(?:src|href)=["\']https?://', out_html, re.I)
print(f'Bundled {out_path} | Size: {kb:.1f} KB ({kb/1024:.2f} MB) | External loads: {len(ext)}')
