#!/usr/bin/env python3
"""Build the release-planning PDF from reviewed, traceable accomplishment records.

Requires reportlab; run from any directory with: python3 scripts/build-accomplishments.py
"""
import json
from pathlib import Path
from xml.sax.saxutils import escape
from reportlab.lib import colors
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.lib.styles import ParagraphStyle
from reportlab.lib.enums import TA_LEFT
from reportlab.platypus import SimpleDocTemplate, Paragraph, Spacer, PageBreak, KeepTogether

ROOT = Path(__file__).resolve().parents[1]
data = json.loads((ROOT / 'docs/accomplishments.json').read_text())
output = ROOT / 'output/pdf/HexManiacAdvance_MacEdition_Accomplishments.pdf'
output.parent.mkdir(parents=True, exist_ok=True)
font_dir = Path('/System/Library/Fonts/Supplemental')
if (font_dir / 'Arial.ttf').exists():
    pdfmetrics.registerFont(TTFont('Helvetica', str(font_dir / 'Arial.ttf')))
    pdfmetrics.registerFont(TTFont('Helvetica-Bold', str(font_dir / 'Arial Bold.ttf')))
    pdfmetrics.registerFontFamily('Helvetica', normal='Helvetica', bold='Helvetica-Bold')
ink = colors.HexColor('#242135')
purple = colors.HexColor('#714AA7')
body = ParagraphStyle('Body', fontName='Helvetica', fontSize=9.3, leading=11.5,
                      textColor=ink, leftIndent=10, firstLineIndent=-8, spaceAfter=4.0)
heading = ParagraphStyle('Section', fontName='Helvetica-Bold', fontSize=14, leading=18,
                         textColor=purple, spaceBefore=9, spaceAfter=9)
note = ParagraphStyle('Note', fontName='Helvetica', fontSize=9, leading=12,
                      textColor=colors.HexColor('#625F70'), spaceAfter=7)

def page_chrome(canvas, doc):
    canvas.saveState()
    canvas.setFillColor(purple)
    canvas.setFont('Helvetica-Bold', 22)
    canvas.drawString(42, 751, 'HexManiacAdvance')
    canvas.setFillColor(ink)
    canvas.setFont('Helvetica', 14)
    canvas.drawString(42, 730, 'Mac Edition')
    canvas.setFont('Helvetica', 10)
    canvas.drawString(42, 710, 'Development accomplishments')
    canvas.setFillColor(colors.HexColor('#625F70'))
    canvas.setFont('Helvetica', 9)
    canvas.drawRightString(570, 732, 'Updated ' + data['date'])
    canvas.setStrokeColor(colors.HexColor('#DCD5E6'))
    canvas.line(42, 697, 570, 697)
    canvas.line(42, 41, 570, 41)
    canvas.setFont('Helvetica', 8)
    canvas.drawString(42, 27, 'Release planning draft  |  Code checkpoint ' + data['checkpoint'])
    canvas.drawRightString(570, 27, f'{doc.page} / {len(data["pages"])}')
    canvas.restoreState()

story=[]
for index, sections in enumerate(data['pages']):
    if index: story.append(PageBreak())
    if index == 0:
        story.append(Paragraph('<b>81 groups / 24,292 assertions / 0 failures / 0 skipped groups</b>', note))
        story.append(Paragraph('Last complete Mac regression: November 8; subsequent font controls passed separate focused checks.', note))
    for section, items in sections:
        story.append(Paragraph(escape(section), heading))
        for title, text, refs in items:
            story.append(Paragraph('&#8226; <b>'+escape(title)+'</b> - '+escape(text), body))
    if index == len(data['pages'])-1:
        story.append(Spacer(1,12))
        story.append(Paragraph('How to read the metrics', heading))
        story.append(Paragraph('Figures describe recorded component or synthetic workloads, not an overall app speedup; timings cannot be added, and temporary allocation differs from retained managed heap and native RAM.', note))
        story.append(Paragraph('Evidence: CodexChanges entries 001-213, the performance reports under tests, and tests/MAP_REFERENCE_MERGE_VERIFICATION.json for the complete run at b6b642f9; later font-control checks are recorded separately in entry 213.', note))
        story.append(Paragraph('The Tutorial Village background is included; the playable tutorial patch and guides remain in development, and HMA patch compatibility with the correct base ROM still awaits user verification.', note))
        story.append(Paragraph('Built on Haven\'s HexManiacAdvance and the original Mac port by xavierkeys; Mac Edition is maintained by arkitekt13.', note))
SimpleDocTemplate(str(output), pagesize=(612,792), leftMargin=42, rightMargin=42,
                  topMargin=106, bottomMargin=52, title='HexManiacAdvance Mac Edition - Development Accomplishments',
                  author='HexManiacAdvance Mac Edition', pageCompression=1).build(story, onFirstPage=page_chrome, onLaterPages=page_chrome)
print(output)
print('Bullets:', sum(len(items) for page in data['pages'] for _,items in page))
