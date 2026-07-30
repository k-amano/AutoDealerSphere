#!/usr/bin/env python3
"""
Document YAML Format v1.1 → HTML Renderer

官公庁フォーマット対応のHTMLレンダラー
- セル結合（colspan/rowspan）
- 複数行ヘッダー
- 行ヘッダー（row_header_cols）
- 改訂履歴・表紙情報
- ページ区切り
- Mermaid図、コードブロック、注釈
"""

import yaml
import html
from datetime import datetime
from pathlib import Path
from typing import Any


class DocumentRenderer:
    """YAML文書をHTMLに変換するレンダラー"""
    
    def __init__(self):
        self.table_count = 0
        self.figure_count = 0
        self.image_count = 0
        
        # 日本語ローカライズ
        self.i18n = {
            'ja': {
                'table': '表',
                'figure': '図',
                'revision_history': '改訂履歴',
                'toc': '目次',
                'version': '版数',
                'date': '日付',
                'author': '作成者',
                'summary': '改訂概要',
                'image_missing': '（画像なし）',
            },
            'en': {
                'table': 'Table',
                'figure': 'Figure',
                'revision_history': 'Revision History',
                'toc': 'Table of Contents',
                'version': 'Version',
                'date': 'Date',
                'author': 'Author',
                'summary': 'Summary',
                'image_missing': '(image missing)',
            }
        }
    
    def _render_title_with_br(self, title: str) -> str:
        """タイトル内の改行を<br>に変換"""
        escaped = html.escape(title)
        return escaped.replace('\n', '<br>')
    
    def render(self, doc: dict) -> str:
        """メインレンダリング処理"""
        self.table_count = 0
        self.figure_count = 0
        self.image_count = 0
        
        lang = doc.get('language', 'ja')
        self.lang = lang
        self.t = self.i18n.get(lang, self.i18n['ja'])
        
        parts = []
        parts.append(self._render_html_head(doc))
        parts.append('<body>')
        
        # 表紙
        parts.append(self._render_cover(doc))
        
        # 改訂履歴
        if 'revisions' in doc:
            parts.append(self._render_revisions(doc))
        
        # ヘッダー
        parts.append(f'<div class="header">{html.escape(doc["title"])}</div>')
        
        # 本文セクション
        for i, section in enumerate(doc.get('sections', []), 1):
            parts.append(self._render_section(section, [i]))
        
        # フッター
        parts.append(f'<div class="footer">生成日時：{datetime.now().strftime("%Y-%m-%d %H:%M")}</div>')
        
        parts.append('</body></html>')
        return '\n'.join(parts)
    
    def _render_html_head(self, doc: dict) -> str:
        """HTMLヘッダーとCSS"""
        title = html.escape(doc.get('title', ''))
        return f'''<!DOCTYPE html>
<html lang="{doc.get('language', 'ja')}">
<head>
    <meta charset="UTF-8">
    <title>{title}</title>
    <style>
        @page {{
            size: A4;
            margin: 20mm 15mm 20mm 15mm;
        }}
        
        /* ゴシック体（タイトル、表、リスト、キャプション用） */
        :root {{
            --font-gothic: "游ゴシック", "Yu Gothic", "YuGothic", "ＭＳ ゴシック", "MS Gothic", sans-serif;
            --font-mincho: "游明朝", "Yu Mincho", "YuMincho", "ＭＳ 明朝", "MS Mincho", serif;
        }}
        
        body {{
            font-family: var(--font-mincho);
            font-size: 10.5pt;
            line-height: 1.5;
            color: #000;
            margin: 0;
            padding: 20px;
            max-width: 210mm;
        }}
        
        /* ヘッダー */
        .header {{
            font-family: var(--font-gothic);
            font-size: 9pt;
            border-bottom: 1pt solid #000;
            padding-bottom: 3pt;
            margin-bottom: 15pt;
        }}
        
        /* 表紙 */
        .cover {{
            font-family: var(--font-gothic);
            page-break-after: always;
            text-align: center;
            padding-top: 100pt;
            min-height: 500pt;
        }}
        .cover-title {{
            font-size: 24pt;
            font-weight: bold;
            color: #00876c;
            margin-bottom: 15pt;
            line-height: 1.4;
        }}
        .cover-subtitle {{
            font-size: 14pt;
            margin-bottom: 40pt;
        }}
        .cover-info {{
            font-size: 12pt;
            margin: 8pt 0;
        }}
        .cover-meta {{
            margin-top: 60pt;
            font-size: 11pt;
        }}
        .cover-meta table {{
            margin: 0 auto;
            border-collapse: collapse;
        }}
        .cover-meta th, .cover-meta td {{
            padding: 4pt 12pt;
            text-align: left;
            border: none;
        }}
        .cover-meta th {{
            background: none;
            font-weight: normal;
        }}
        
        /* 改訂履歴ページ */
        .revision-page {{
            page-break-after: always;
        }}
        .revision-title {{
            font-family: var(--font-gothic);
            font-size: 14pt;
            font-weight: bold;
            text-align: center;
            margin-bottom: 20pt;
        }}
        
        /* 見出し1 - 緑背景帯 */
        h1 {{
            font-family: var(--font-gothic);
            font-size: 14pt;
            font-weight: bold;
            margin: 20pt 0 10pt 0;
            padding: 6pt 10pt;
            background-color: #00876c;
            color: #fff;
        }}
        
        /* 見出し2 - 左に青縦線 */
        h2 {{
            font-family: var(--font-gothic);
            font-size: 12pt;
            font-weight: bold;
            margin: 15pt 0 8pt 0;
            padding: 4pt 0 4pt 10pt;
            border-left: 4pt solid #2e75b6;
            background-color: #f0f0f0;
        }}
        
        /* 見出し3 */
        h3 {{
            font-family: var(--font-gothic);
            font-size: 11pt;
            font-weight: bold;
            margin: 12pt 0 6pt 0;
        }}
        
        /* 段落（本文は明朝体） */
        p {{
            margin: 6pt 0;
            text-align: justify;
        }}
        
        /* 表キャプション */
        .table-caption {{
            font-family: var(--font-gothic);
            font-size: 10pt;
            font-weight: bold;
            margin: 10pt 0 4pt 0;
        }}
        
        /* 図キャプション */
        .figure-caption {{
            font-family: var(--font-gothic);
            font-size: 10pt;
            margin: 4pt 0 10pt 0;
        }}
        
        /* 表 */
        table {{
            font-family: var(--font-gothic);
            width: 100%;
            border-collapse: collapse;
            margin: 0 0 10pt 0;
            font-size: 10pt;
        }}
        th, td {{
            border: 1pt solid #000;
            padding: 4pt 6pt;
            text-align: left;
            vertical-align: top;
        }}
        th {{
            background-color: #d9d9d9;
            font-weight: normal;
            text-align: center;
        }}
        .row-header {{
            background-color: #d9d9d9;
            text-align: center;
        }}
        
        /* リスト */
        ul, ol {{
            font-family: var(--font-gothic);
            margin: 6pt 0 6pt 25pt;
            padding: 0;
        }}
        li {{
            margin: 3pt 0;
        }}
        
        /* Mermaid図 */
        .mermaid-container {{
            margin: 10pt 0;
            text-align: center;
        }}
        .mermaid {{
            display: inline-block;
        }}
        
        /* コードブロック */
        .code-block {{
            background-color: #f5f5f5;
            border: 1pt solid #ccc;
            padding: 8pt 10pt;
            margin: 8pt 0;
            font-family: "ＭＳ ゴシック", "MS Gothic", monospace;
            font-size: 9pt;
            white-space: pre-wrap;
            overflow-x: auto;
        }}
        .code-caption {{
            font-size: 10pt;
            margin: 8pt 0 4pt 0;
        }}
        
        /* 注釈 */
        .note {{
            margin: 10pt 0;
            padding: 8pt 12pt;
            border-left: 4pt solid #666;
            background-color: #f9f9f9;
        }}
        .note-info {{
            border-left-color: #2e75b6;
            background-color: #e8f4fc;
        }}
        .note-warning {{
            border-left-color: #e6a700;
            background-color: #fff8e6;
        }}
        .note-important {{
            border-left-color: #c00;
            background-color: #fee;
        }}
        
        /* 画像 */
        .image-container {{
            margin: 10pt 0;
        }}
        .image-container.align-center {{
            text-align: center;
        }}
        .image-container.align-right {{
            text-align: right;
        }}
        .image-container img {{
            max-width: 100%;
        }}
        .image-missing {{
            display: inline-block;
            padding: 20pt 40pt;
            border: 1pt dashed #999;
            color: #666;
            background-color: #f5f5f5;
        }}
        
        /* ページ区切り */
        .page-break {{
            page-break-before: always;
        }}
        
        /* フッター */
        .footer {{
            margin-top: 30pt;
            font-size: 9pt;
            text-align: right;
            color: #666;
            border-top: 1pt solid #ccc;
            padding-top: 5pt;
        }}
    </style>
    <script src="https://cdn.jsdelivr.net/npm/mermaid/dist/mermaid.min.js"></script>
    <script>mermaid.initialize({{startOnLoad: true}});</script>
</head>'''
    
    def _render_cover(self, doc: dict) -> str:
        """表紙のレンダリング"""
        cover = doc.get('cover', {})
        title = self._render_title_with_br(doc.get('title', ''))
        
        parts = ['<div class="cover">']
        parts.append(f'<div class="cover-title">{title}</div>')
        
        if 'subtitle' in cover:
            parts.append(f'<div class="cover-subtitle">{html.escape(cover["subtitle"])}</div>')
        
        if 'version' in cover or 'date' in cover:
            info = []
            if 'version' in cover:
                info.append(f'第 {html.escape(cover["version"])} 版')
            if 'date' in cover:
                info.append(html.escape(cover['date']))
            parts.append(f'<div class="cover-info">{" ".join(info)}</div>')
        
        # メタ情報テーブル
        meta_rows = []
        if 'organization' in cover:
            meta_rows.append(('組織', cover['organization']))
        if 'department' in cover:
            meta_rows.append(('部署', cover['department']))
        if 'author' in cover:
            meta_rows.append(('作成者', cover['author']))
        if 'approver' in cover:
            meta_rows.append(('承認者', cover['approver']))
        
        if meta_rows:
            parts.append('<div class="cover-meta"><table>')
            for label, value in meta_rows:
                parts.append(f'<tr><th>{label}：</th><td>{html.escape(value)}</td></tr>')
            parts.append('</table></div>')
        
        parts.append('</div>')
        return '\n'.join(parts)
    
    def _render_revisions(self, doc: dict) -> str:
        """改訂履歴ページのレンダリング"""
        revisions = doc.get('revisions', [])
        if not revisions:
            return ''
        
        parts = ['<div class="revision-page">']
        parts.append(f'<div class="header">{html.escape(doc["title"])}</div>')
        parts.append(f'<div class="revision-title">{self.t["revision_history"]}</div>')
        
        self.table_count += 1
        parts.append(f'<div class="table-caption">{self.t["table"]} {self.table_count} {self.t["revision_history"]}</div>')
        parts.append('<table>')
        parts.append(f'<tr><th style="width:60pt;">{self.t["version"]}</th>')
        parts.append(f'<th style="width:80pt;">{self.t["date"]}</th>')
        parts.append(f'<th style="width:80pt;">{self.t["author"]}</th>')
        parts.append(f'<th>{self.t["summary"]}</th></tr>')
        
        for rev in revisions:
            version = html.escape(str(rev.get('version', '')))
            date = html.escape(str(rev.get('date', '')))
            author = html.escape(str(rev.get('author', '')))
            summary = html.escape(str(rev.get('summary', '')))
            parts.append(f'<tr><td style="text-align:center;">{version}</td>')
            parts.append(f'<td style="text-align:center;">{date}</td>')
            parts.append(f'<td>{author}</td>')
            parts.append(f'<td>{summary}</td></tr>')
        
        parts.append('</table>')
        parts.append('</div>')
        return '\n'.join(parts)
    
    def _render_section(self, section: dict, numbers: list[int]) -> str:
        """セクションのレンダリング（再帰）"""
        level = len(numbers)
        number_str = '.'.join(map(str, numbers))
        title = self._render_title_with_br(section.get('title', ''))
        
        parts = []
        
        # ページ区切り
        if section.get('page_break_before'):
            parts.append('<div class="page-break"></div>')
        
        # 見出し
        h_level = min(level, 3)
        parts.append(f'<h{h_level}>{number_str}. {title}</h{h_level}>')
        
        # ブロック
        for block in section.get('blocks', []):
            parts.append(self._render_block(block, number_str))
        
        # 子セクション
        for i, child in enumerate(section.get('sections', []), 1):
            parts.append(self._render_section(child, numbers + [i]))
        
        return '\n'.join(parts)
    
    def _render_block(self, block: dict, section_num: str) -> str:
        """ブロックのレンダリング"""
        block_type = block.get('type')
        
        if block_type == 'text':
            return self._render_text(block)
        elif block_type == 'table':
            return self._render_table(block, section_num)
        elif block_type == 'list':
            return self._render_list(block)
        elif block_type == 'figure':
            return self._render_figure(block)
        elif block_type == 'image':
            return self._render_image(block)
        elif block_type == 'code':
            return self._render_code(block)
        elif block_type == 'note':
            return self._render_note(block)
        else:
            return f'<!-- unknown block type: {block_type} -->'
    
    def _render_text(self, block: dict) -> str:
        """テキストブロック"""
        content = block.get('content', '')
        paragraphs = content.strip().split('\n\n')
        parts = []
        for para in paragraphs:
            escaped = html.escape(para.strip()).replace('\n', '<br>')
            parts.append(f'<p>{escaped}</p>')
        return '\n'.join(parts)
    
    def _render_table(self, block: dict, section_num: str) -> str:
        """表ブロック（セル結合対応）"""
        self.table_count += 1
        caption = block.get('caption', '')
        header = block.get('header', [])
        rows = block.get('rows', [])
        row_header_cols = block.get('row_header_cols', 0)
        
        parts = []
        
        # キャプション
        caption_text = f'{self.t["table"]} {section_num}-{self.table_count}'
        if caption:
            caption_text += f' {html.escape(caption)}'
        parts.append(f'<div class="table-caption">{caption_text}</div>')
        
        parts.append('<table>')
        
        # ヘッダー行
        if header:
            # 複数行ヘッダー対応
            if header and isinstance(header[0], list):
                # 2次元配列
                for row in header:
                    parts.append('<tr>')
                    for cell in row:
                        parts.append(self._render_header_cell(cell))
                    parts.append('</tr>')
            else:
                # 1次元配列（従来形式）
                parts.append('<tr>')
                for cell in header:
                    parts.append(f'<th>{html.escape(str(cell))}</th>')
                parts.append('</tr>')
        
        # データ行
        # rowspanを追跡するための配列
        rowspan_remaining = {}
        
        for row in rows:
            parts.append('<tr>')
            col_idx = 0
            cell_idx = 0
            
            while cell_idx < len(row) or col_idx in rowspan_remaining:
                # rowspanが継続中のセルをスキップ
                while col_idx in rowspan_remaining and rowspan_remaining[col_idx] > 0:
                    rowspan_remaining[col_idx] -= 1
                    if rowspan_remaining[col_idx] == 0:
                        del rowspan_remaining[col_idx]
                    col_idx += 1
                
                if cell_idx >= len(row):
                    break
                
                cell = row[cell_idx]
                is_row_header = col_idx < row_header_cols
                parts.append(self._render_data_cell(cell, is_row_header, col_idx, rowspan_remaining))
                
                # colspanを考慮
                colspan = 1
                if isinstance(cell, dict):
                    colspan = cell.get('colspan', 1)
                col_idx += colspan
                cell_idx += 1
            
            parts.append('</tr>')
        
        parts.append('</table>')
        return '\n'.join(parts)
    
    def _render_header_cell(self, cell: Any) -> str:
        """ヘッダーセルのレンダリング"""
        if isinstance(cell, dict):
            value = html.escape(str(cell.get('value', '')))
            attrs = []
            if 'colspan' in cell:
                attrs.append(f'colspan="{cell["colspan"]}"')
            if 'rowspan' in cell:
                attrs.append(f'rowspan="{cell["rowspan"]}"')
            attr_str = ' ' + ' '.join(attrs) if attrs else ''
            return f'<th{attr_str}>{value}</th>'
        else:
            return f'<th>{html.escape(str(cell))}</th>'
    
    def _render_data_cell(self, cell: Any, is_row_header: bool, col_idx: int, rowspan_remaining: dict) -> str:
        """データセルのレンダリング"""
        if isinstance(cell, dict):
            value = cell.get('value')
            if value is None:
                value = ''
            value = html.escape(str(value))
            
            attrs = []
            if 'colspan' in cell:
                attrs.append(f'colspan="{cell["colspan"]}"')
            if 'rowspan' in cell:
                rowspan = cell['rowspan']
                attrs.append(f'rowspan="{rowspan}"')
                # rowspan追跡を更新
                rowspan_remaining[col_idx] = rowspan - 1
            
            if is_row_header:
                attrs.append('class="row-header"')
            
            attr_str = ' ' + ' '.join(attrs) if attrs else ''
            tag = 'th' if is_row_header else 'td'
            return f'<{tag}{attr_str}>{value}</{tag}>'
        else:
            value = '' if cell is None else html.escape(str(cell))
            if is_row_header:
                return f'<th class="row-header">{value}</th>'
            else:
                return f'<td>{value}</td>'
    
    def _render_list(self, block: dict) -> str:
        """リストブロック"""
        style = block.get('style', 'bullet')
        items = block.get('items', [])
        tag = 'ol' if style == 'number' else 'ul'
        
        return f'<{tag}>\n{self._render_list_items(items, tag)}\n</{tag}>'
    
    def _render_list_items(self, items: list, tag: str) -> str:
        """リストアイテムの再帰レンダリング"""
        parts = []
        for item in items:
            if isinstance(item, str):
                parts.append(f'<li>{html.escape(item)}</li>')
            elif isinstance(item, dict):
                text = html.escape(item.get('text', ''))
                children = item.get('children', [])
                if children:
                    child_html = self._render_list_items(children, tag)
                    parts.append(f'<li>{text}\n<{tag}>\n{child_html}\n</{tag}>\n</li>')
                else:
                    parts.append(f'<li>{text}</li>')
        return '\n'.join(parts)
    
    def _render_figure(self, block: dict) -> str:
        """図ブロック（Mermaid）"""
        self.figure_count += 1
        caption = block.get('caption', '')
        code = block.get('code', '')
        
        parts = ['<div class="mermaid-container">']
        parts.append(f'<div class="mermaid">\n{code}\n</div>')
        
        caption_text = f'{self.t["figure"]} {self.figure_count}'
        if caption:
            caption_text += f' {html.escape(caption)}'
        parts.append(f'<div class="figure-caption">{caption_text}</div>')
        parts.append('</div>')
        
        return '\n'.join(parts)
    
    def _render_image(self, block: dict) -> str:
        """画像ブロック"""
        self.image_count += 1
        path = block.get('path', '')
        caption = block.get('caption', '')
        alt = block.get('alt', caption)
        width = block.get('width', '')
        align = block.get('align', 'left')
        
        align_class = f' align-{align}' if align != 'left' else ''
        parts = [f'<div class="image-container{align_class}">']
        
        # 画像が存在するかチェック（簡易版：パスがあれば表示）
        style = f' style="width:{width};"' if width else ''
        parts.append(f'<img src="{html.escape(path)}" alt="{html.escape(alt)}"{style}>')
        
        caption_text = f'{self.t["figure"]} {self.image_count}'
        if caption:
            caption_text += f' {html.escape(caption)}'
        parts.append(f'<div class="figure-caption">{caption_text}</div>')
        parts.append('</div>')
        
        return '\n'.join(parts)
    
    def _render_code(self, block: dict) -> str:
        """コードブロック"""
        content = block.get('content', '')
        caption = block.get('caption', '')
        language = block.get('language', '')
        
        parts = []
        if caption:
            parts.append(f'<div class="code-caption">{html.escape(caption)}</div>')
        
        parts.append(f'<pre class="code-block"><code>{html.escape(content)}</code></pre>')
        return '\n'.join(parts)
    
    def _render_note(self, block: dict) -> str:
        """注釈ブロック"""
        content = block.get('content', '')
        style = block.get('style', 'info')
        
        escaped = html.escape(content.strip()).replace('\n', '<br>')
        return f'<div class="note note-{style}">{escaped}</div>'


def render_yaml_to_html(yaml_content: str) -> str:
    """YAML文字列をHTMLに変換"""
    doc = yaml.safe_load(yaml_content)
    renderer = DocumentRenderer()
    return renderer.render(doc)


def render_file(input_path: str, output_path: str = None) -> str:
    """YAMLファイルをHTMLファイルに変換"""
    input_path = Path(input_path)
    
    with open(input_path, 'r', encoding='utf-8') as f:
        yaml_content = f.read()
    
    html_content = render_yaml_to_html(yaml_content)
    
    if output_path is None:
        output_path = input_path.with_suffix('.html')
    else:
        output_path = Path(output_path)
    
    with open(output_path, 'w', encoding='utf-8') as f:
        f.write(html_content)
    
    return str(output_path)


if __name__ == '__main__':
    import sys
    
    if len(sys.argv) < 2:
        print("Usage: python renderer.py <input.yaml> [output.html]")
        sys.exit(1)
    
    input_file = sys.argv[1]
    output_file = sys.argv[2] if len(sys.argv) > 2 else None
    
    result = render_file(input_file, output_file)
    print(f"Generated: {result}")
