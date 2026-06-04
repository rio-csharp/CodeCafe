import GitHubIcon from '@/shared/ui/icons/GitHubIcon'
import { useTranslation } from 'react-i18next'

function Footer() {
  const { t } = useTranslation()
  return (
    <footer className="border-t border-border-subtle">
      <div className="py-6">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8 flex items-center justify-between text-sm">
          <span className="text-text-tertiary">
            © {new Date().getFullYear()} {t('footer.copyright')}
          </span>

          <a
            href="https://github.com/rio-csharp/CodeCafe"
            target="_blank"
            rel="noopener noreferrer"
            className="text-text-tertiary hover:text-text-primary transition-colors"
            aria-label="GitHub"
          >
            <GitHubIcon className="h-5 w-5" />
          </a>
        </div>
      </div>
    </footer>
  )
}

export default Footer
