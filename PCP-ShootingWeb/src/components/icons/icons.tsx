import type { ReactNode, SVGProps } from 'react';

/*
 * Íconos de trazo copiados de los diseños (fuente-dc): viewBox 24, trazo 1.8, color = currentColor.
 * Los marcados "nuevo" no existen en los diseños (pantallas sin diseño todavía).
 */
export type IconProps = Omit<SVGProps<SVGSVGElement>, 'children'> & { size?: number };
export type IconComponent = (props: IconProps) => ReactNode;

function createIcon(paths: ReactNode, strokeWidth = 1.8): IconComponent {
  return function Icon({ size = 22, ...rest }: IconProps) {
    return (
      <svg
        width={size}
        height={size}
        viewBox="0 0 24 24"
        fill="none"
        stroke="currentColor"
        strokeWidth={strokeWidth}
        strokeLinecap="round"
        strokeLinejoin="round"
        aria-hidden="true"
        focusable="false"
        {...rest}
      >
        {paths}
      </svg>
    );
  };
}

export const IconMenu = createIcon(<path d="M4 7h16M4 12h16M4 17h10" />);
export const IconClose = createIcon(<path d="M6 6l12 12M18 6 6 18" />, 2);
export const IconChevronRight = createIcon(<path d="m9 6 6 6-6 6" />, 2);
export const IconChevronLeft = createIcon(<path d="m15 6-6 6 6 6" />);
export const IconPlus = createIcon(<path d="M12 5v14M5 12h14" />, 2.5);
export const IconMinus = createIcon(<path d="M5 12h14" />, 2.5);
export const IconCheck = createIcon(<path d="m5 12 5 5L20 7" />, 2.5);
export const IconArrowRight = createIcon(<path d="M5 12h14M13 6l6 6-6 6" />, 2);
export const IconSearch = createIcon(
  <>
    <circle cx="11" cy="11" r="7" />
    <path d="m20 20-3.5-3.5" />
  </>,
);
export const IconArrowLeft = createIcon(<path d="M19 12H5M11 6l-6 6 6 6" />, 2);
export const IconPin = createIcon(
  <>
    <path d="M12 21s7-6.2 7-12a7 7 0 0 0-14 0c0 5.8 7 12 7 12z" />
    <circle cx="12" cy="9" r="2.5" />
  </>,
);
export const IconClock = createIcon(
  <>
    <circle cx="12" cy="12" r="9" />
    <path d="M12 7v5l3 2" />
  </>,
);
export const IconPhone = createIcon(
  <path d="M5 4h4l2 5-2.5 1.5a11 11 0 0 0 5 5L15 13l5 2v4a2 2 0 0 1-2 2A16 16 0 0 1 3 6a2 2 0 0 1 2-2z" />,
);
export const IconMail = createIcon(
  <>
    <rect x="3" y="5" width="18" height="14" rx="2" />
    <path d="m3 7 9 6 9-6" />
  </>,
);
export const IconChat = createIcon(<path d="M4 20l1.5-4A8 8 0 1 1 9 19.4z" />);
export const IconPlay = createIcon(<path d="M8 5v14l11-7z" fill="currentColor" stroke="none" />);
export const IconQuote = createIcon(
  <path d="M4 17h4l2-4V7H4v6h3zm9 0h4l2-4V7h-6v6h3z" fill="currentColor" stroke="none" />,
);
export const IconEye = createIcon(
  <>
    <path d="M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7S2 12 2 12z" />
    <circle cx="12" cy="12" r="3" />
  </>,
);
export const IconEyeOff = createIcon(
  <>
    <path d="M10.6 5.1A10 10 0 0 1 12 5c6.5 0 10 7 10 7a17 17 0 0 1-2.6 3.4M6.6 6.6A17 17 0 0 0 2 12s3.5 7 10 7a9.7 9.7 0 0 0 5.4-1.6" />
    <path d="M9.9 9.9a3 3 0 0 0 4.2 4.2M3 3l18 18" />
  </>,
);
export const IconQr = createIcon(
  <>
    <rect x="3" y="3" width="7" height="7" rx="1" />
    <rect x="14" y="3" width="7" height="7" rx="1" />
    <rect x="3" y="14" width="7" height="7" rx="1" />
    <path d="M14 14h3v3h-3zM20 14v.01M14 20h.01M17 20h4v-3" />
  </>,
);
export const IconLogout = createIcon(
  <>
    <path d="M15 4h4a1 1 0 0 1 1 1v14a1 1 0 0 1-1 1h-4" />
    <path d="M10 17l-5-5 5-5M5 12h11" />
  </>,
);
export const IconLock = createIcon(
  <>
    <rect x="5" y="11" width="14" height="10" rx="2" />
    <path d="M8 11V8a4 4 0 0 1 8 0v3" />
  </>,
);
export const IconFile = createIcon(
  <>
    <path d="M14 3H6a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V9z" />
    <path d="M14 3v6h6M8 13h8M8 17h5" />
  </>,
);
export const IconCalendarPlus = createIcon(
  <>
    <rect x="3" y="5" width="18" height="16" rx="2" />
    <path d="M3 10h18M8 3v4M16 3v4M12 13v5M9.5 15.5h5" />
  </>,
);
export const IconBadge = createIcon(
  <>
    <circle cx="12" cy="12" r="8" />
    <circle cx="12" cy="12" r="3" />
    <path d="M12 2v3M12 19v3M2 12h3M19 12h3" />
  </>,
);
export const IconUpload = createIcon(<path d="M12 16V4M7 9l5-5 5 5M4 20h16" />, 2);
export const IconDownload = createIcon(<path d="M12 4v12M7 11l5 5 5-5M4 20h16" />, 2);
export const IconInbox = createIcon(
  <>
    <path d="M3 13h5l1.5 3h5L16 13h5" />
    <path d="M5 5h14l2 8v6H3v-6z" />
  </>,
  1.5,
);

export const IconUser = createIcon(
  <>
    <circle cx="12" cy="8" r="4" />
    <path d="M4 21c0-4 4-6 8-6s8 2 8 6" />
  </>,
);
export const IconCart = createIcon(
  <>
    <circle cx="9" cy="20" r="1.5" />
    <circle cx="18" cy="20" r="1.5" />
    <path d="M2 3h3l2.5 12h12l2-8H6.2" />
  </>,
);

/* Barra de pestañas pública */
export const IconTarget = createIcon(
  <>
    <circle cx="12" cy="12" r="9" />
    <circle cx="12" cy="12" r="4" />
    <path d="M12 1v4M12 19v4M1 12h4M19 12h4" />
  </>,
);
export const IconStar = createIcon(
  <path d="M12 3l2.6 5.3 5.9.9-4.3 4.1 1 5.8L12 16.4 6.8 19.1l1-5.8L3.5 9.2l5.9-.9z" />,
);
export const IconShirt = createIcon(<path d="M8 3 3 6l2 5 3-1v11h8V10l3 1 2-5-5-3a4 4 0 0 1-8 0z" />);
export const IconCrown = createIcon(<path d="m3 8 4 4 5-7 5 7 4-4-2 11H5z" />);

/* Panel administrativo */
export const IconHome = createIcon(
  <>
    <path d="M3 11 12 4l9 7" />
    <path d="M5 10v10h14V10" />
  </>,
);
export const IconCalendar = createIcon(
  <>
    <rect x="3" y="5" width="18" height="16" rx="2" />
    <path d="M3 10h18M8 3v4M16 3v4" />
  </>,
);
export const IconBag = createIcon(
  <>
    <path d="M6 7h12l-1 13H7z" />
    <path d="M9 7a3 3 0 0 1 6 0" />
  </>,
);
export const IconBox = createIcon(
  <>
    <path d="M4 7l8-4 8 4-8 4z" />
    <path d="M4 7v10l8 4 8-4V7" />
  </>,
);
export const IconUsers = createIcon(
  <>
    <circle cx="9" cy="8" r="3.5" />
    <path d="M2 20c0-3.5 3-5.5 7-5.5s7 2 7 5.5M16 4.5a3.5 3.5 0 0 1 0 7M22 20c0-2.6-1.6-4.4-4-5.1" />
  </>,
);
export const IconDocument = createIcon(
  <>
    <path d="M4 4h16v16H4z" />
    <path d="M8 9h8M8 13h8M8 17h5" />
  </>,
);
export const IconGear = createIcon(
  <>
    <circle cx="12" cy="12" r="3" />
    <path d="M12 2v3M12 19v3M2 12h3M19 12h3M4.9 4.9 7 7M17 17l2.1 2.1M4.9 19.1 7 17M17 7l2.1-2.1" />
  </>,
);
/* nuevo: paquetes de tiro */
export const IconTicket = createIcon(
  <>
    <path d="M3 8a2 2 0 0 0 0 4v0a2 2 0 0 1 0 4v2h18v-2a2 2 0 0 1 0-4v0a2 2 0 0 0 0-4V6H3z" />
    <path d="M14 6v12" strokeDasharray="2 2" />
  </>,
);
/* nuevo: alimentos y bebidas */
export const IconCup = createIcon(
  <>
    <path d="M4 8h13v5a6 6 0 0 1-6 6h-1a6 6 0 0 1-6-6z" />
    <path d="M17 10h1.5a2.5 2.5 0 0 1 0 5H17M8 2v3M12 2v3" />
  </>,
);
/* nuevo: inventario */
export const IconClipboard = createIcon(
  <>
    <rect x="5" y="4" width="14" height="17" rx="2" />
    <path d="M9 4V3h6v1M9 10h6M9 14h6M9 18h3" />
  </>,
);
/* nuevo: reportes */
export const IconChart = createIcon(<path d="M4 20V10M10 20V4M16 20v-7M22 20H2" />);
/* nuevo: "Más" en la barra del panel */
export const IconGrid = createIcon(
  <>
    <rect x="3" y="3" width="7" height="7" rx="1" />
    <rect x="14" y="3" width="7" height="7" rx="1" />
    <rect x="3" y="14" width="7" height="7" rx="1" />
    <rect x="14" y="14" width="7" height="7" rx="1" />
  </>,
);
