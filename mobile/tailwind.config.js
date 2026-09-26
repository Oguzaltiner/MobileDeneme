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
      },
    },
  },
  plugins: [],
};
