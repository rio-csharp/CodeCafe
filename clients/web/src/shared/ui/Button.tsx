import type { ButtonHTMLAttributes } from 'react'
import { buttonClass } from './buttonClass'

export type ButtonVariant = 'primary' | 'ghost'

export interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: ButtonVariant
}

export function Button({ variant = 'primary', className, type = 'button', ...rest }: ButtonProps) {
  return <button type={type} className={buttonClass(variant, className)} {...rest} />
}
