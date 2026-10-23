export interface SeoInput { title: string; slug: string; summary: string; body: string; metaTitle?: string; metaDescription?: string; focusKeyword?: string }
export interface SeoCheck { label: string; ok: boolean; hint: string }

const words = (s: string) => (s.match(/\S+/g) ?? []).length

/** On-page SEO checklist shown while editing a guide. These are rules of thumb, not a ranking guarantee. */
export function seoChecks(a: SeoInput): SeoCheck[] {
  const title = a.metaTitle || a.title
  const desc = a.metaDescription || a.summary
  const kw = (a.focusKeyword ?? '').trim().toLowerCase()
  const firstPara = a.body.split(/\n{2,}/).find(b => !b.startsWith('#'))?.toLowerCase() ?? ''
  const checks: SeoCheck[] = [
    { label: 'Title length 30-60 characters', ok: title.length >= 30 && title.length <= 60, hint: `Now ${title.length}. Longer titles get cut off in search results.` },
    { label: 'Description length 70-160 characters', ok: desc.length >= 70 && desc.length <= 160, hint: `Now ${desc.length}. Write a clear sentence that makes people want to click.` },
    { label: 'At least 300 words', ok: words(a.body) >= 300, hint: `Now ${words(a.body)}. Thin pages rarely rank and AdSense may reject them.` },
    { label: 'At least 2 subheadings (## Heading)', ok: (a.body.match(/^## /gm) ?? []).length >= 2, hint: 'Subheadings make the page easier to scan and to rank for related questions.' },
    { label: 'Short, readable URL slug', ok: /^[a-z0-9]+(-[a-z0-9]+){0,5}$/.test(a.slug) && a.slug.length <= 60, hint: 'Lowercase words separated by hyphens, six words at most.' },
  ]
  if (kw) {
    checks.push(
      { label: 'Focus keyword in the title', ok: title.toLowerCase().includes(kw), hint: `Add "${kw}" to the title.` },
      { label: 'Focus keyword in the description', ok: desc.toLowerCase().includes(kw), hint: `Mention "${kw}" in the description.` },
      { label: 'Focus keyword in the URL slug', ok: a.slug.includes(kw.replace(/\s+/g, '-')), hint: `Use "${kw.replace(/\s+/g, '-')}" in the slug.` },
      { label: 'Focus keyword in the first paragraph', ok: firstPara.includes(kw), hint: 'Use it naturally in the opening paragraph.' },
    )
  } else checks.push({ label: 'Focus keyword set', ok: false, hint: 'Pick the phrase a parent would search for, e.g. "potty training regression".' })
  return checks
}
