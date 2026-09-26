/** @type {import('tailwindcss').Config} */
module.exports = {
  content: ['./App.{js,jsx,ts,tsx}', './src/**/*.{js,jsx,ts,tsx}'],
  presets: [require('nativewind/preset')],
  theme: {
    extend: {
      colors: {
        background: '#F8FAFC',
        foreground: '#0F172A',
        'muted-foreground': '#64748B',
        surface: '#FFFFFF',
        border: '#E2E8F0',
        primary: '#2563EB',
        'primary-soft': '#EFF6FF',
        accent: '#F59E0B',
      },
    },
  },
  plugins: [],
};
