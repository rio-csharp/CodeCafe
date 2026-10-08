import { describe, expect, it, vi } from 'vitest'
import { AiChatHttpError, streamChat } from './streamChat'

function sseStream(chunks: string[]): ReadableStream<Uint8Array> {
  return new ReadableStream({
    start(controller) {
      for (const chunk of chunks) {
        controller.enqueue(new TextEncoder().encode(chunk))
      }
      controller.close()
    },
  })
}

function stubFetch(response: Response) {
  const fetchMock = vi.fn().mockResolvedValue(response)
  vi.stubGlobal('fetch', fetchMock)
  return fetchMock
}

describe('streamChat', () => {
  it('posts the conversation and parses SSE frames, even split across chunks', async () => {
    stubFetch(
      new Response(
        sseStream([
          'event: text\ndata: {"kind":"text","text":"Hel',
          'lo"}\n\nevent: tool-call\ndata: {"kind":"tool-call","tool":"get_page","callId":"c1","arguments":{}}\n\n',
          'event: done\ndata: {"kind":"done"}\n\n',
        ]),
        { status: 200 },
      ),
    )
    const events: unknown[] = []

    await streamChat({
      slug: 'my-notebook',
      messages: [{ role: 'User', content: 'hi' }],
      signal: new AbortController().signal,
      onEvent: (event) => {
        events.push(event)
      },
    })

    expect(events).toEqual([
      { kind: 'text', text: 'Hello' },
      { kind: 'tool-call', tool: 'get_page', callId: 'c1' },
      { kind: 'done' },
    ])
    expect(fetch).toHaveBeenCalledWith(
      '/api/notebooks/my-notebook/ai/chat',
      expect.objectContaining({ method: 'POST' }),
    )
  })

  it('parses error frames like any other event', async () => {
    stubFetch(
      new Response(
        sseStream(['event: error\ndata: {"kind":"error","code":"ai_disabled","message":"off"}\n\n']),
        { status: 200 },
      ),
    )
    const events: unknown[] = []

    await streamChat({
      slug: 's',
      messages: [],
      signal: new AbortController().signal,
      onEvent: (event) => {
        events.push(event)
      },
    })

    expect(events).toEqual([{ kind: 'error', code: 'ai_disabled', message: 'off' }])
  })

  it('throws an AiChatHttpError with the envelope details on pre-stream failure', async () => {
    stubFetch(
      new Response(JSON.stringify({ error: { code: 'notebook_not_found', message: 'nope' } }), {
        status: 404,
        headers: { 'Content-Type': 'application/json' },
      }),
    )

    const failure = await streamChat({
      slug: 'ghost',
      messages: [],
      signal: new AbortController().signal,
      onEvent: () => {},
    }).catch((error: unknown) => error)

    expect(failure).toBeInstanceOf(AiChatHttpError)
    expect((failure as AiChatHttpError).status).toBe(404)
    expect((failure as AiChatHttpError).code).toBe('notebook_not_found')
  })

  it('skips malformed frames instead of killing the stream', async () => {
    stubFetch(
      new Response(
        sseStream([
          'data: {not json}\n\n',
          'event: text\ndata: {"kind":"text","text":"ok"}\n\n',
        ]),
        { status: 200 },
      ),
    )
    const events: unknown[] = []

    await streamChat({
      slug: 's',
      messages: [],
      signal: new AbortController().signal,
      onEvent: (event) => {
        events.push(event)
      },
    })

    expect(events).toEqual([{ kind: 'text', text: 'ok' }])
  })
})
