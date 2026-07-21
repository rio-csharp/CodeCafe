import '@testing-library/jest-dom/vitest'

// React 19 + Testing Library: opt into act() support in the test environment
// to silence "The current testing environment is not configured to support act(...)".
// @ts-expect-error — IS_REACT_ACT_ENVIRONMENT is a React-internal test flag.
globalThis.IS_REACT_ACT_ENVIRONMENT = true
