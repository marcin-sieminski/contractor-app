/** @type {import('tailwindcss').Config} */
export default {
  content: ['./index.html', './src/**/*.{ts,tsx}'],
  darkMode: 'class',
  theme: {
    extend: {
      // Dodatkowy breakpoint dla bardzo wąskich ekranów (małe telefony).
      // Domyślne breakpointy Tailwind (sm/md/lg/xl/2xl) pozostają nietknięte.
      screens: {
        xs: '480px'
      }
    }
  },
  plugins: []
}
