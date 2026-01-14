'use client';

import {Select} from "@heroui/select";
import {SelectItem} from "@heroui/react";

type Props = {
    answerCount: number;
}
export default function AnswersHeader({ answerCount }: Props) {
    return (
        <div className='flex items-center justify-between pt-3 w-full px-6'>
            <div className='text-2xl'>{answerCount} {answerCount === 1 ? 'answer' : 'Answers'}</div>
            <div className='flex items-center gap-3 justify-end w-[50%] ml-auto'>
                <Select 
                    aria-label='Select sorting'
                    defaultSelectedKeys={['highScore']}
                >
                    <SelectItem key='highScore'>Highest score (default)</SelectItem>
                    <SelectItem key='created'>Date Created</SelectItem>
                </Select>
            </div>
        </div>
    );
}