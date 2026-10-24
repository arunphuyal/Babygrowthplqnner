import test from 'node:test'
import assert from 'node:assert/strict'
import { parseBody, parseInline, readMinutes, linkedGuideSlugs, plainText } from './markup.mjs'

test('paragraphs, headings and lists', () => {
  const b = parseBody('Intro line\nsecond line\n\n## Head\n- a\n- b\n\n- c\n- d')
  assert.deepEqual(b.map(x => x.type), ['p', 'h2', 'ul', 'ul'])
  assert.equal(plainText(b), 'Intro line second line Head a b c d')
})

test('a sentence directly above a list stays a paragraph followed by a list', () => {
  const b = parseBody('You wait for signs.\n- Pros: calm\n- Cons: slow\nThen you adjust.')
  assert.deepEqual(b.map(x => x.type), ['p', 'ul', 'p'])
  assert.equal(b[1].items.length, 2)
})

test('a heading may be followed directly by a list or text', () => {
  const b = parseBody('## Signs\n- one\n- two')
  assert.deepEqual(b.map(x => x.type), ['h2', 'ul'])
  assert.equal(parseBody('## Title\nfollow-on text')[1].type, 'p')
})

test('internal and https links are kept, everything else becomes plain text', () => {
  const p = parseInline('[ok](/guides/a) [ext](https://example.com/x) [bad](javascript:alert(1)) [proto](//evil.com) [http](http://x.com)')
  assert.deepEqual(p.filter(x => x.t === 'link').map(x => [x.href, x.external]), [['/guides/a', false], ['https://example.com/x', true]])
  const text = p.filter(x => x.t === 'text').map(x => x.v).join('|')
  assert.ok(text.includes('bad') && text.includes('proto') && text.includes('http'))
  assert.ok(!p.some(x => x.t === 'link' && /javascript|\/\/evil/.test(x.href)))
})

test('html in text is left as text for the renderers to escape', () => {
  assert.equal(parseInline('<img src=x onerror=alert(1)>')[0].v, '<img src=x onerror=alert(1)>')
})

test('linked guide slugs ignore other links and duplicates', () => {
  assert.deepEqual(linkedGuideSlugs('[a](/guides/one) [b](/guides/two) [c](/guides/one) [d](https://x.com) [e](/privacy)'), ['one', 'two'])
})

test('read time is at least one minute', () => {
  assert.equal(readMinutes('short'), 1)
  assert.equal(readMinutes('word '.repeat(1000)), 5)
})

test('empty and odd input does not throw', () => {
  assert.deepEqual(parseBody(''), [])
  assert.deepEqual(parseBody(null), [])
  assert.equal(parseBody('\n\n\n').length, 0)
})
