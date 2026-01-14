'use client'

import {forwardRef} from "react";
import Link, {LinkProps} from 'next/link';

export const LinkCompoennt = forwardRef<HTMLAnchorElement, LinkProps>(function LinkCompoent(props, ref) {
    return <Link ref={ref} {...props} />;    
});