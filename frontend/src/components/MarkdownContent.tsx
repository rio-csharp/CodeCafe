import { useState } from 'react'
import ReactMarkdown from 'react-markdown'
import type { Components } from 'react-markdown'
import { Prism as SyntaxHighlighter } from 'react-syntax-highlighter'
import { oneLight } from 'react-syntax-highlighter/dist/esm/styles/prism'
import { coldarkDark } from 'react-syntax-highlighter/dist/esm/styles/prism'
import remarkGfm from 'remark-gfm'
import { useTheme } from '../app/useTheme'

function CopyButton({ text }: { text: string }) {
  const [copied, setCopied] = useState(false)

  const handleCopy = () => {
    navigator.clipboard.writeText(text).then(() => {
      setCopied(true)
      setTimeout(() => setCopied(false), 2000)
    })
  }

  return (
    <button
      aria-label={copied ? '已复制' : '复制代码'}
      className={`code-copy-btn${copied ? ' code-copy-btn--copied' : ''}`}
      onClick={handleCopy}
      type="button"
    >
      {copied ? (
        // checkmark
        <svg fill="none" height="16" stroke="currentColor" strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" viewBox="0 0 24 24" width="16">
          <polyline points="20 6 9 17 4 12" />
        </svg>
      ) : (
        // copy
        <svg fill="none" height="16" stroke="currentColor" strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" viewBox="0 0 24 24" width="16">
          <rect height="13" rx="2" ry="2" width="13" x="9" y="9" />
          <path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1" />
        </svg>
      )}
    </button>
  )
}

export function MarkdownContent({
  children,
  rehypePlugins,
}: {
  children: string
  rehypePlugins?: NonNullable<React.ComponentProps<typeof ReactMarkdown>['rehypePlugins']>
}) {
  const { theme } = useTheme()
  const codeTheme = theme === 'light' ? oneLight : coldarkDark

  const components: Components = {
    code(props) {
      const { children: codeChildren, className, ...rest } = props
      const match = /language-(\w+)/.exec(className ?? '')
      const codeText = String(codeChildren).replace(/\n$/, '')

      if (!match) {
        return (
          <code className="inline-code" {...rest}>
            {codeChildren}
          </code>
        )
      }

      return (
        <div className="code-block-wrapper">
          <CopyButton text={codeText} />
          <SyntaxHighlighter
            codeTagProps={{
              style: {
                background: 'transparent',
              },
            }}
            customStyle={{
              background: 'transparent',
              color: 'inherit',
              margin: 0,
              padding: 0,
              paddingRight: '44px',
            }}
            language={match[1]}
            PreTag="div"
            style={codeTheme}
            useInlineStyles
          >
            {codeText}
          </SyntaxHighlighter>
        </div>
      )
    },
  }

  return (
    <ReactMarkdown components={components} rehypePlugins={rehypePlugins} remarkPlugins={[remarkGfm]}>
      {children}
    </ReactMarkdown>
  )
}
