import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { Providers } from './providers'
import './styles/index.css'

const container = document.getElementById('root')
if (!container) {
  throw new Error('index.html is missing its #root container')
}

createRoot(container).render(
  <StrictMode>
    <Providers />
  </StrictMode>,
)
