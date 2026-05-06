import ReactMarkdown from 'react-markdown'
import type { Components } from 'react-markdown'
import { Prism as SyntaxHighlighter } from 'react-syntax-highlighter'
import { oneLight } from 'react-syntax-highlighter/dist/esm/styles/prism'
import { coldarkDark } from 'react-syntax-highlighter/dist/esm/styles/prism'
import remarkGfm from 'remark-gfm'
import { useTheme } from '../app/useTheme'

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
          }}
          language={match[1]}
          PreTag="div"
          style={codeTheme}
          useInlineStyles
        >
          {codeText}
        </SyntaxHighlighter>
      )
    },
  }

  return (
    <ReactMarkdown components={components} rehypePlugins={rehypePlugins} remarkPlugins={[remarkGfm]}>
      {children}
    </ReactMarkdown>
  )
}
