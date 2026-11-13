from __future__ import annotations

import re
from pathlib import Path

from reportlab.lib import colors
from reportlab.lib.enums import TA_CENTER, TA_LEFT
from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import ParagraphStyle, getSampleStyleSheet
from reportlab.lib.units import mm
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.platypus import (
    BaseDocTemplate,
    Frame,
    KeepTogether,
    PageBreak,
    PageTemplate,
    Paragraph,
    Spacer,
)


ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "docs" / "user-manual.md"
OUTPUT = ROOT / "output" / "pdf" / "ST_Holmes-Manual-de-usuario.pdf"

NAVY = colors.HexColor("#0F172A")
SLATE = colors.HexColor("#334155")
MUTED = colors.HexColor("#64748B")
BLUE = colors.HexColor("#2563EB")
PALE = colors.HexColor("#EFF6FF")
GREEN = colors.HexColor("#16A34A")
AMBER = colors.HexColor("#D97706")
RED = colors.HexColor("#DC2626")
WHITE = colors.white


def register_fonts() -> tuple[str, str, str]:
    fonts = Path("C:/Windows/Fonts")
    regular = fonts / "segoeui.ttf"
    semibold = fonts / "seguisb.ttf"
    bold = fonts / "segoeuib.ttf"
    if not (regular.exists() and semibold.exists() and bold.exists()):
        return "Helvetica", "Helvetica-Bold", "Helvetica-Bold"
    pdfmetrics.registerFont(TTFont("Segoe", regular))
    pdfmetrics.registerFont(TTFont("SegoeSemi", semibold))
    pdfmetrics.registerFont(TTFont("SegoeBold", bold))
    return "Segoe", "SegoeSemi", "SegoeBold"


FONT, FONT_SEMI, FONT_BOLD = register_fonts()


class ManualDocTemplate(BaseDocTemplate):
    def __init__(self, filename: str):
        super().__init__(
            filename,
            pagesize=A4,
            leftMargin=22 * mm,
            rightMargin=22 * mm,
            topMargin=23 * mm,
            bottomMargin=20 * mm,
            title="ST_Holmes - Manual de usuario",
            author="Proyecto ST_Holmes",
            subject="Instalacion, uso y mantenimiento de ST_Holmes",
        )
        frame = Frame(
            self.leftMargin,
            self.bottomMargin,
            self.width,
            self.height,
            id="content",
        )
        self.addPageTemplates(PageTemplate(id="manual", frames=[frame], onPage=draw_page))


def draw_page(canvas, doc):
    page = canvas.getPageNumber()
    if page == 1:
        canvas.saveState()
        canvas.setFillColor(NAVY)
        canvas.rect(0, 0, A4[0], A4[1], fill=1, stroke=0)
        canvas.setFillColor(BLUE)
        canvas.rect(0, A4[1] - 11 * mm, A4[0], 11 * mm, fill=1, stroke=0)
        canvas.restoreState()
        return
    canvas.saveState()
    canvas.setStrokeColor(colors.HexColor("#CBD5E1"))
    canvas.setLineWidth(0.5)
    canvas.line(22 * mm, 18 * mm, A4[0] - 22 * mm, 18 * mm)
    canvas.setFillColor(MUTED)
    canvas.setFont(FONT, 8)
    canvas.drawString(22 * mm, 11 * mm, "ST_Holmes - Manual de usuario")
    canvas.drawRightString(A4[0] - 22 * mm, 11 * mm, f"Pagina {page}")
    canvas.restoreState()


def styles():
    base = getSampleStyleSheet()
    return {
        "body": ParagraphStyle(
            "Body",
            parent=base["BodyText"],
            fontName=FONT,
            fontSize=10.2,
            leading=15.2,
            textColor=SLATE,
            spaceAfter=7,
        ),
        "h1": ParagraphStyle(
            "H1",
            parent=base["Heading1"],
            fontName=FONT_BOLD,
            fontSize=19,
            leading=23,
            textColor=NAVY,
            spaceBefore=8,
            spaceAfter=10,
            keepWithNext=True,
        ),
        "h2": ParagraphStyle(
            "H2",
            parent=base["Heading2"],
            fontName=FONT_SEMI,
            fontSize=12.5,
            leading=16,
            textColor=BLUE,
            spaceBefore=10,
            spaceAfter=5,
            keepWithNext=True,
        ),
        "bullet": ParagraphStyle(
            "Bullet",
            parent=base["BodyText"],
            fontName=FONT,
            fontSize=10.1,
            leading=14.7,
            textColor=SLATE,
            leftIndent=13,
            firstLineIndent=-8,
            bulletIndent=2,
            spaceAfter=4,
        ),
        "step": ParagraphStyle(
            "Step",
            parent=base["BodyText"],
            fontName=FONT,
            fontSize=10.1,
            leading=14.7,
            textColor=SLATE,
            leftIndent=15,
            firstLineIndent=-12,
            spaceAfter=5,
        ),
        "cover_title": ParagraphStyle(
            "CoverTitle",
            fontName=FONT_BOLD,
            fontSize=31,
            leading=35,
            textColor=WHITE,
            alignment=TA_LEFT,
        ),
        "cover_sub": ParagraphStyle(
            "CoverSub",
            fontName=FONT,
            fontSize=13,
            leading=19,
            textColor=colors.HexColor("#DBEAFE"),
        ),
        "toc": ParagraphStyle(
            "Toc",
            fontName=FONT,
            fontSize=10.5,
            leading=18,
            textColor=SLATE,
            leftIndent=3,
        ),
        "small": ParagraphStyle(
            "Small",
            fontName=FONT,
            fontSize=8.5,
            leading=12,
            textColor=MUTED,
            alignment=TA_CENTER,
        ),
    }


def markup(text: str) -> str:
    text = text.replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;")
    text = re.sub(r"\*\*(.+?)\*\*", r"<b>\1</b>", text)
    text = re.sub(r"`(.+?)`", r"<font name='Courier'>\1</font>", text)
    return text


def cover(st):
    return [
        Spacer(1, 25 * mm),
        Paragraph("ST_Holmes", st["cover_title"]),
        Spacer(1, 5 * mm),
        Paragraph("Manual de usuario", st["cover_title"]),
        Spacer(1, 11 * mm),
        Paragraph(
            "Supervision preventiva y trayectoria SMART para tus unidades internas.",
            st["cover_sub"],
        ),
        Spacer(1, 61 * mm),
        Paragraph("Version 1.0 - Windows 10/11 (64 bits)", st["cover_sub"]),
        Spacer(1, 8 * mm),
        Paragraph(
            "Software gratuito y de codigo abierto. Toda la informacion permanece en el equipo.",
            st["cover_sub"],
        ),
        PageBreak(),
    ]


def parse_source(st):
    lines = SOURCE.read_text(encoding="utf-8").splitlines()
    sections = []
    current = None
    for raw in lines:
        if raw.startswith("# ") or raw.startswith("**Version"):
            continue
        if raw.startswith("## "):
            current = {"title": raw[3:], "lines": []}
            sections.append(current)
        elif current is not None:
            current["lines"].append(raw)

    story = []
    story.append(Paragraph("Contenido", st["h1"]))
    for section in sections:
        story.append(Paragraph(markup(section["title"]), st["toc"]))
    story.append(PageBreak())

    for section in sections:
        story.append(Paragraph(markup(section["title"]), st["h1"]))
        paragraph = []
        for raw in section["lines"] + [""]:
            line = raw.strip()
            if line.startswith("### "):
                if paragraph:
                    story.append(Paragraph(markup(" ".join(paragraph)), st["body"]))
                    paragraph = []
                story.append(Paragraph(markup(line[4:]), st["h2"]))
            elif re.match(r"^\d+\. ", line):
                if paragraph:
                    story.append(Paragraph(markup(" ".join(paragraph)), st["body"]))
                    paragraph = []
                number, text = line.split(". ", 1)
                story.append(Paragraph(f"<b>{number}.</b> {markup(text)}", st["step"]))
            elif line.startswith("- "):
                if paragraph:
                    story.append(Paragraph(markup(" ".join(paragraph)), st["body"]))
                    paragraph = []
                story.append(Paragraph(markup("- " + line[2:]), st["bullet"]))
            elif not line:
                if paragraph:
                    story.append(Paragraph(markup(" ".join(paragraph)), st["body"]))
                    paragraph = []
            else:
                paragraph.append(line)
        story.append(Spacer(1, 3 * mm))
    return story


def build():
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    st = styles()
    doc = ManualDocTemplate(str(OUTPUT))
    story = cover(st) + parse_source(st)
    doc.build(story)
    print(OUTPUT)


if __name__ == "__main__":
    build()

