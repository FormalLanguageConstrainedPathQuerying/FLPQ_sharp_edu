"""mdformat plugin: preserve ordered list start numbers; support GFM tables.

mdformat's default ``ordered_list`` renderer writes the list's start number
on the first item only and renders every following item as a zero-padded
``1`` (e.g. ``101.`` / ``001.`` / ``001.``). That destroys ordered lists
whose numbers are data, such as task entries in ``tasks/tasks*.md``.

This plugin overrides the ``ordered_list`` renderer with consecutive
numbering from the list's start number and no zero padding. The logic
mirrors mdformat 1.0.0's own ``consecutive_numbering`` branch (option
``number=True``, see ``mdformat.renderer._context.ordered_list``) minus
the padding, so behavior stays aligned with upstream.

mdformat also parses with markdown-it's default preset, which has no GFM
table support: a table row is parsed as a paragraph and the formatter
unescapes ``\\|`` cell separators, silently destroying tables. This plugin
enables the ``table`` rule in ``update_mdit`` and provides the missing
renderers (mdformat ships none): rows/cells are rendered in canonical form
and literal pipes in cell text are re-escaped so the table keeps its
columns.

Part of the project's mdformat toolchain: install it in every environment
that runs mdformat on this repo (local, CI) so all ordered lists keep their
explicit numbering and all GFM tables survive formatting::

    pip install -e tools/mdformat_tasklog
"""

from mdformat.renderer._context import code_inline as _default_code_inline
from mdformat.renderer._context import text as _default_text
from mdformat.renderer._util import get_list_marker_type, is_tight_list


def hr(node, context) -> str:
    """Render thematic breaks as ``---`` instead of a 70-underscore line."""
    return "---"


def ordered_list(node, context) -> str:
    """Render an ordered list with consecutive numbering, no zero padding."""
    marker_type = get_list_marker_type(node)
    first_line_indent = " "
    block_separator = "\n" if is_tight_list(node) else "\n\n"

    starting_number = node.attrs.get("start")
    if starting_number is None:
        starting_number = 1

    # Same as upstream's consecutive branch: width of the LAST number, so
    # continuation lines stay inside items even when digit count grows.
    last_number = starting_number + len(node.children) - 1
    indent_width = len(f"{last_number}{marker_type}{first_line_indent}")

    text = ""
    with context.indented(indent_width):
        for list_item_index, list_item in enumerate(node.children):
            list_item_text = list_item.render(context)
            formatted_lines = []
            line_iterator = iter(list_item_text.split("\n"))
            first_line = next(line_iterator)
            number = starting_number + list_item_index
            marker = f"{number}{marker_type}"
            formatted_lines.append(
                f"{marker}{first_line_indent}{first_line}" if first_line else marker
            )
            for line in line_iterator:
                formatted_lines.append(" " * indent_width + line if line else "")

            text += "\n".join(formatted_lines)
            if list_item_index != len(node.children) - 1:
                text += block_separator

    return text


def _in_table_cell(node) -> bool:
    parent = node.parent
    while parent is not None:
        if parent.type in ("th", "td"):
            return True
        parent = parent.parent
    return False


def code_inline(node, context) -> str:
    """Default code span rendering; re-escape pipes inside table cells.

    The parser's line-level cell splitting consumes ``\\|`` before inline
    parsing, so a code span containing a pipe only survives re-parsing if the
    escape is kept in the source. Re-adding it changes neither the rendered
    content (the splitter consumes it again) nor the table structure.
    """
    rendered = _default_code_inline(node, context)
    if _in_table_cell(node):
        rendered = rendered.replace("|", "\\|")
    return rendered


def text(node, context) -> str:
    """Default text rendering; re-escape literal pipes inside table cells.

    Without this, a cell containing a literal pipe (e.g. a grammar with an
    alternative, ``S -> a | b``) renders an unescaped ``|`` and the table
    gains spurious columns on the next parse. Code spans and HTML are other
    token types and are not touched, so their content stays literal.
    """
    rendered = _default_text(node, context)
    if _in_table_cell(node):
        rendered = rendered.replace("|", "\\|")
    return rendered


def _align_marker(align: str) -> str:
    if align == "left":
        return ":---"
    if align == "center":
        return ":---:"
    if align == "right":
        return "---:"
    return "---"


def table_cell(node, context) -> str:
    """Render a th/td cell (its inline children)."""
    return "".join(child.render(context) for child in node.children)


def table_row(node, context) -> str:
    cells = [child.render(context) for child in node.children]
    return "| " + " | ".join(cells) + " |"


def thead(node, context) -> str:
    return node.children[0].render(context)


def tbody(node, context) -> str:
    return "\n".join(row.render(context) for row in node.children)


def table(node, context) -> str:
    """Render a GFM table; synthesize the separator row from header alignment."""
    rows = []
    for child in node.children:
        if child.type == "thead":
            header_row = child.children[0]
            rows.append(header_row.render(context))
            aligns = [
                (cell.attrs or {}).get("style", "").replace("text-align:", "")
                for cell in header_row.children
            ]
            rows.append("| " + " | ".join(_align_marker(a) for a in aligns) + " |")
        elif child.type == "tbody":
            rows.extend(row.render(context) for row in child.children)
    return "\n".join(rows)


class TaskLogExtension:
    """Preserve ordered list start numbers; enable and render GFM tables."""

    CHANGES_AST = False
    RENDERERS = {
        "ordered_list": ordered_list,
        "hr": hr,
        "text": text,
        "code_inline": code_inline,
        "table": table,
        "thead": thead,
        "tbody": tbody,
        "tr": table_row,
        "th": table_cell,
        "td": table_cell,
    }

    @staticmethod
    def update_mdit(mdit) -> None:
        mdit.enable("table")
