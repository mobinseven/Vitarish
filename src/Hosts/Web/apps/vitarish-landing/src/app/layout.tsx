import type {Metadata} from 'next';
import {Vazirmatn} from 'next/font/google';
import './globals.css';

const vazirmatn = Vazirmatn({subsets: ['arabic'], variable: '--font-vazirmatn', display: 'swap'});

export const metadata: Metadata = {title: 'Vitarish — The glory of purpose', description: 'Purpose-oriented AI services for people and small businesses.'};
export default function RootLayout({children}:{children:React.ReactNode}) { return <html lang="en" dir="ltr" className={vazirmatn.variable}><body>{children}</body></html>; }
