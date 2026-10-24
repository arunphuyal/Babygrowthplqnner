// Guide text format shared by the website, the prerender script and the admin preview (admin/src/markup.mjs must be an identical
// copy: CI fails if they differ). Supported: blank-line paragraphs, "## " headings, "- " bullet lists and [label](link).
// Links are allowlisted: site-relative ("/guides/x") or https only. Anything else renders as plain text, so a guide can never
// contain a javascript: or protocol-relative link.

const LINK = /\[([^\]]+)\]\(([^)\s]+)\)/g
const SAFE_HREF = /^(\/(?!\/)[^\s]*|https:\/\/[^\s]+)$/

/** @param {string} text @returns {Array<{t:'text',v:string}|{t:'link',v:string,href:string,external:boolean}>} */
export function parseInline(text) {
  const out = []
  let last = 0
  for (const m of text.matchAll(LINK)) {
    if (m.index > last) out.push({ t: 'text', v: text.slice(last, m.index) })
    if (SAFE_HREF.test(m[2])) out.push({ t: 'link', v: m[1], href: m[2], external: !m[2].startsWith('/') })
    else out.push({ t: 'text', v: m[1] })
    last = m.index + m[0].length
  }
  if (last < text.length) out.push({ t: 'text', v: text.slice(last) })
  return out
}

/** @param {string} text */
export function parseBody(text) {
  const blocks = []
  // Consecutive "- " lines become one list; consecutive other lines become one paragraph, so a sentence directly above a list works.
  const runs = lines => {
    let i = 0
    while (i < lines.length) {
      const bullet = lines[i].startsWith('- ')
      let j = i
      while (j < lines.length && lines[j].startsWith('- ') === bullet) j++
      const chunk = lines.slice(i, j)
      blocks.push(bullet ? { type: 'ul', items: chunk.map(l => parseInline(l.slice(2))) } : { type: 'p', inline: parseInline(chunk.join(' ')) })
      i = j
    }
  }
  for (const raw of String(text ?? '').split(/\n{2,}/)) {
    const block = raw.trim()
    if (!block) continue
    const lines = block.split('\n').map(l => l.trimEnd())
    if (lines[0].startsWith('## ')) { blocks.push({ type: 'h2', text: lines[0].slice(3).trim() }); runs(lines.slice(1)) } else runs(lines)
  }
  return blocks
}

/** Plain text of a parsed body, for word counts and read time. */
export function plainText(blocks) {
  const inl = parts => parts.map(p => p.v).join('')
  return blocks.map(b => b.type === 'h2' ? b.text : b.type === 'p' ? inl(b.inline) : b.items.map(inl).join(' ')).join(' ')
}

export const readMinutes = text => Math.max(1, Math.round((String(text ?? '').match(/\S+/g) ?? []).length / 200))

/** Slugs of the site-internal guides a body links to, in order of first appearance. */
export function linkedGuideSlugs(text) {
  const slugs = []
  for (const m of String(text ?? '').matchAll(LINK)) {
    const g = /^\/guides\/([a-z0-9-]+)$/.exec(m[2])
    if (g && !slugs.includes(g[1])) slugs.push(g[1])
  }
  return slugs
}
